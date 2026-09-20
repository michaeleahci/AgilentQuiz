# 仪器资源预约管理系统 - 设计文档

## 1. 项目概述

基于 **.NET 8 + SQL Server (EF Core)** 实现的仪器资源预约管理系统，采用分层架构（Domain / Application / Infrastructure / Api），面向接口设计，支持高并发预约、违约自动处理、仪器故障/报废联动通知等核心业务。

## 2. 技术栈

| 类别 | 选型 |
|------|------|
| 框架 | .NET 8 (ASP.NET Core Web API) |
| 数据库 | SQL Server 2022 |
| ORM | Entity Framework Core 8 |
| 依赖注入 | 内置 DI |
| 测试 | xUnit + Moq |
| 定时任务 | IHostedService (后台服务) |

## 3. 分层架构

```
AgilentQuiz.sln
├── src/
│   ├── AgilentQuiz.Domain/          # 领域层：实体、枚举、仓储接口、领域服务接口
│   ├── AgilentQuiz.Application/     # 应用层：DTO、应用服务、业务编排
│   ├── AgilentQuiz.Infrastructure/  # 基础设施层：EF Core、仓储实现、后台任务
│   └── AgilentQuiz.Api/             # API 层：控制器、中间件、DI 配置
├── tests/
│   └── AgilentQuiz.Tests/           # 单元测试
├── database/
│   ├── init.sql                     # EF Core 生成的建表脚本
│   └── seed.sql                     # 种子数据（A/B/C 类型 + 6 台仪器）
└── docker-compose.yml               # SQL Server 容器
```

**依赖方向**：Api → Application → Domain，Infrastructure 实现 Domain 定义的接口。

## 4. 领域模型设计

### 4.1 实体关系

```
User (实验人员)
  │ 1
  │
  │ N
Reservation (预约单) ──N:1──> InstrumentType (仪器类型)
  │ 1                          │ 1
  │                            │
  │ N                          │ N
ReservationItem (预约明细) ──N:1──> Instrument (仪器)
  │
  │ N
Notification (通知记录) <──N:1── User
```

### 4.2 核心实体说明

| 实体 | 关键字段 | 说明 |
|------|----------|------|
| **User** | Phone(唯一), BanExpiryTime | 手机号作为业务标识；BanExpiryTime 记录违约禁用到期时间 |
| **InstrumentType** | Code(唯一), Status | A/B/C 等类型；Status: Enabled/Disabled |
| **Instrument** | Code(唯一), Status, RowVersion | 单台设备；Status: Available/Faulty/Scrapped；RowVersion 乐观并发 |
| **Reservation** | InstrumentTypeId, StartTime, EndTime, Status, IdempotencyKey, RowVersion | 单次预约仅一种类型；IdempotencyKey 防重复提交 |
| **ReservationItem** | InstrumentId, Status | 预约单中的单台仪器明细 |
| **Notification** | Type, Content, Status, RetryCount | 通知记录；支持失败重试 |

### 4.3 状态流转

**预约单状态**：`Pending(待使用) → InUse(使用中) → Completed(已完成)`，或 `Pending → Cancelled(已取消)`，或 `Pending → Defaulted(违约)`。

**预约项状态**：与预约单联动，支持部分取消（部分项 Cancelled，其余仍 Pending）。

**违约判定规则**：定时任务每分钟扫描，若预约单状态为 `Pending` 且 `EndTime < 当前时间`，则标记为 `Defaulted`，并对用户禁用 24 小时。

## 5. 数据库设计

### 5.1 核心表

- `Users` — 用户（手机号唯一索引）
- `InstrumentTypes` — 仪器类型（Code 唯一索引）
- `Instruments` — 仪器（Code 唯一索引；RowVersion 乐观锁）
- `Reservations` — 预约单（IdempotencyKey 过滤唯一索引；Phone+Status 索引；InstrumentTypeId+Status+StartTime+EndTime 复合索引）
- `ReservationItems` — 预约明细（InstrumentId+Status 索引）
- `Notifications` — 通知记录（Status+RetryCount 索引）

### 5.2 索引设计思路

| 索引 | 用途 |
|------|------|
| `IX_Users_Phone` (唯一) | 按手机号查询/去重 |
| `IX_Reservations_PhoneStatus` | 按手机号查有效/历史预约 |
| `IX_Reservations_TypeStatusTime` | 冲突检测核心查询：同类型+活跃状态+时间段 |
| `IX_Reservations_IdempotencyKey` (唯一, 过滤NULL) | 幂等键防重复 |
| `IX_ReservationItems_InstrumentStatus` | 按仪器查有效预约（故障/报废联动） |
| `IX_Notifications_StatusRetry` | 定时任务扫描待发送通知 |

## 6. 并发控制（防超卖）

### 6.1 核心策略：Serializable 事务 + UPDLOCK/HOLDLOCK

预约创建流程在 **Serializable 隔离级别**事务内执行：

1. 开启 Serializable 事务
2. 执行冲突检测 SQL，使用 `WITH (UPDLOCK, HOLDLOCK)` 锁定相关行与索引范围
3. 若无冲突，插入预约单与明细
4. 提交事务

**原理**：Serializable 隔离级别下的键范围锁（Key-Range Locking）+ UPDLOCK 提示，确保两个并发请求不会同时通过冲突检测并插入同一仪器的同一时间段预约，从根本上避免超卖。

冲突检测 SQL：
```sql
SELECT DISTINCT ri.InstrumentId
FROM ReservationItems ri WITH (UPDLOCK, HOLDLOCK)
INNER JOIN Reservations r WITH (UPDLOCK, HOLDLOCK) ON ri.ReservationId = r.Id
WHERE r.InstrumentTypeId = @typeId
  AND r.Status IN (1, 2)        -- Pending, InUse
  AND ri.Status IN (1, 2)       -- Pending, Completed
  AND r.StartTime < @endTime
  AND r.EndTime > @startTime
```

### 6.2 乐观并发

`Instrument` 和 `Reservation` 表使用 `RowVersion`（SQL Server rowversion 类型），EF Core 配置为 `IsRowVersion()`，更新时自动检测并发冲突。

### 6.3 幂等性

创建预约接口支持 `IdempotencyKey`：数据库层对 `IdempotencyKey` 建唯一索引（过滤 NULL），应用层先查后插，重复请求直接返回已有预约。

## 7. 业务规则实现

### 7.1 预约校验（领域服务）
- 必须至少提前 **1 小时** 预约
- 用户不在违约禁用期内
- 仪器类型已启用
- 仪器状态为 Available
- 所选时间段内仪器无冲突

### 7.2 取消规则
- 仅 `Pending` 状态可取消
- 支持整单取消或按仪器ID部分取消
- 部分取消后若所有明细均取消，预约单整体变为 Cancelled

### 7.3 违约处理（定时任务）
- `DefaultHandlingHostedService` 每分钟执行
- 扫描 `Pending` 且 `EndTime < now` 的预约
- 标记违约 + 禁用用户 24 小时 + 发送通知

### 7.4 仪器故障/报废联动
- **故障**：标记故障 → 查找受影响有效预约 → 创建通知（提示重新预约/改约）
- **报废**：标记报废 → 自动取消受影响预约项 → 创建通知

## 8. 健壮性与可靠性设计

| 场景 | 处理方式 |
|------|----------|
| 重复提交 | 幂等键 + 唯一索引 |
| 网络异常 | EF Core 自动重试（EnableRetryOnFailure 5次） |
| 服务重启 | 定时任务基于数据库状态，重启后继续扫描 |
| 通知失败 | Notification 表记录 RetryCount，定时任务重试（最多3次） |
| 数据一致性 | 故障/报废处理在同一事务内完成状态变更+通知创建 |
| 定时任务异常 | 全局 try-catch，记录日志，下一轮继续 |

## 9. 性能优化

- **查询优化**：针对核心查询（冲突检测、按手机号查预约、按仪器查预约）建立复合索引
- **缓存思路**（可扩展）：仪器类型/仪器基础信息可加 Redis 缓存，冲突检测仍走数据库保证强一致
- **连接弹性**：EF Core 配置 `EnableRetryOnFailure` 应对瞬态故障
- **分页**：历史预约查询当前返回全量，生产环境应加分页参数

## 10. API 设计

### 10.1 实验人员接口

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/reservations` | 创建预约（支持 IdempotencyKey） |
| GET | `/api/reservations/{id}` | 预约详情 |
| GET | `/api/reservations/active?phone=` | 当前有效预约 |
| GET | `/api/reservations/history?phone=` | 历史预约 |
| POST | `/api/reservations/{id}/cancel` | 取消预约（整单/部分） |

### 10.2 管理员接口

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/instrument-types` | 新增仪器类型 |
| PUT | `/api/instrument-types/{id}` | 修改仪器类型 |
| POST | `/api/instrument-types/{id}/disable` | 禁用仪器类型 |
| GET | `/api/instrument-types` | 仪器类型列表 |
| POST | `/api/instruments` | 新增仪器 |
| PUT | `/api/instruments/{id}` | 修改仪器 |
| POST | `/api/instruments/{id}/fault` | 标记故障 |
| POST | `/api/instruments/{id}/scrap` | 标记报废 |
| POST | `/api/instruments/{id}/recover` | 恢复故障 |
| GET | `/api/instruments/usage` | 资源使用情况 |

### 10.3 统一响应格式

```json
{
  "code": "0",
  "message": "success",
  "data": { ... }
}
```

### 10.4 错误码

| 码 | 说明 |
|----|------|
| 0 | 成功 |
| 1001 | 参数错误 |
| 1002 | 资源不存在 |
| 2001 | 预约需提前1小时 |
| 2002 | 用户违约禁用中 |
| 2003 | 仪器类型已禁用 |
| 2004 | 仪器不可用 |
| 2005 | 仪器时间段冲突 |
| 2006 | 预约单不存在 |
| 2007 | 预约不可取消 |

## 11. 运行方式

### 11.1 启动 SQL Server

```bash
docker-compose up -d
```

### 11.2 初始化数据库

数据库迁移在开发环境启动时自动执行（`Program.cs` 中 `dbContext.Database.Migrate()`）。
也可手动执行 `database/init.sql` 和 `database/seed.sql`。

### 11.3 运行 API

```bash
dotnet run --project src/AgilentQuiz.Api
```

Swagger 地址：`https://localhost:5001/swagger`

### 11.4 运行测试

```bash
dotnet test
```

## 12. 扩展思考（高级工程师）

针对未来 50+ 类型 / 1000+ 仪器 / 多实验室 / 跨地区等场景，架构调整建议：

| 扩展方向 | 调整方案 |
|----------|----------|
| 数据量增长 | Reservation 表按时间分区（表分区）；冷热数据分离 |
| 高并发预约 | 引入 Redis 分布式锁 + 数据库行锁双保险；预约请求异步化（消息队列削峰） |
| 多实验室 | InstrumentType/Instrument 增加 LabId，数据按实验室隔离；查询加 LabId 过滤 |
| 跨地区部署 | 读写分离 + CDN；数据库多活或主从同步 |
| 通知渠道 | INotificationSender 接口多实现（企业微信/短信/邮件），工厂模式按用户偏好路由 |
| 仪器控制对接 | 新增 IInstrumentControlService 抽象层，通过适配器模式对接不同厂商协议 |
| AI 推荐 | 基于历史预约数据训练模型，推荐空闲时段/替代仪器；与预约服务解耦，通过 API 调用 |

## 13. AI 辅助说明

- **使用工具**：Trae CN（内置代码助手）
- **参与环节**：代码生成、架构设计、数据库设计、测试编写
- **AI 生成代码占比**：约 90%（核心业务逻辑、实体、仓储、控制器由 AI 生成；架构决策由人工确认）
- **质量验证**：dotnet build 0 错误 0 警告；22 个单元测试全部通过；数据库脚本人工审核

## 14. 未完成/简化说明

- 登录/认证/授权：按题目要求未实现
- 通知发送：使用 Mock 实现（题目允许外部依赖 Mock）
- 分页：历史预约查询未加分页（生产环境需补充）
- 分布式锁：当前依赖数据库 Serializable 事务，高并发场景可引入 Redis
