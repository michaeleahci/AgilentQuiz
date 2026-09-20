# 仪器资源预约管理系统 - 技术架构文档

> 本文档详细说明系统每一个功能的技术实现，包括调用链路、核心代码位置、关键算法与数据流向。

---

## 一、系统概述

本系统是一套基于 **.NET 8 + SQL Server** 的实验室仪器资源预约管理系统，采用经典四层分层架构，面向接口设计，支持高并发预约防超卖、违约自动处理、仪器状态联动通知等业务场景。

**核心能力**：
- 实验人员：预约创建/查询/取消、违约自动禁用
- 管理员：仪器类型与仪器增删改查、故障/报废状态联动、资源使用看板
- 系统：通知自动分发、违约定时扫描、幂等防重、并发防超卖

---

## 二、技术架构总览

### 2.1 分层架构

```
┌─────────────────────────────────────────────────────────────┐
│                        API 层 (Presentation)                 │
│  Controllers / Middleware / DI 配置 / Program.cs            │
│  职责：HTTP 请求接收、模型校验、统一响应、全局异常捕获         │
├─────────────────────────────────────────────────────────────┤
│                     Application 层 (Application)             │
│  DTOs / AppServices / DomainService / Common                │
│  职责：业务编排、DTO 映射、事务边界、调用领域规则             │
├─────────────────────────────────────────────────────────────┤
│                     Domain 层 (Domain)                       │
│  Entities / Enums / Interfaces(IRepository/IService)        │
│  职责：核心业务模型、领域规则、状态机、不依赖任何外部框架     │
├─────────────────────────────────────────────────────────────┤
│                   Infrastructure 层 (Infrastructure)         │
│  EF Core DbContext / Repositories / UnitOfWork             │
│  HostedServices / MockNotificationSender                    │
│  职责：数据持久化、事务管理、外部依赖适配、定时任务           │
└─────────────────────────────────────────────────────────────┘
         │                                    │
         ▼                                    ▼
   SQL Server 2022                   外部通知服务（Mock）
```

**依赖方向**：`Api → Application → Domain`，`Infrastructure` 实现 `Domain` 中定义的接口，通过 DI 注入到 `Application`。

### 2.2 项目结构

| 项目 | 职责 |
|------|------|
| `AgilentQuiz.Domain` | 实体、枚举、仓储接口、领域服务接口 |
| `AgilentQuiz.Application` | DTO、应用服务、领域服务实现、公共组件（错误码/异常/统一响应） |
| `AgilentQuiz.Infrastructure` | EF Core DbContext、仓储实现、UnitOfWork、定时任务、通知 Mock |
| `AgilentQuiz.Api` | 控制器、全局异常中间件、Swagger、Program.cs 启动配置 |
| `AgilentQuiz.Tests` | xUnit 单元测试（22 个用例） |

---

## 三、功能模块详细说明

### 3.1 实验人员模块

#### 3.1.1 创建预约

**入口**：`POST /api/reservations` → [ReservationsController.Create](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Api/Controllers/ReservationsController.cs#L21-L32)

**调用链路**：

```
Controller → ReservationAppService.CreateAsync
  ├─ 1. 幂等键检查（GetByIdempotencyKeyAsync）
  ├─ 2. 获取或创建用户（GetOrCreateUserAsync）
  ├─ 3. 校验仪器类型存在且启用
  ├─ 4. 构建 Reservation 实体 + ReservationItem 明细
  └─ 5. ExecuteInTransactionAsync（Serializable 事务）
       ├─ ReservationDomainService.ValidateReservationAsync（冲突检测）
       ├─ ReservationRepository.AddAsync
       └─ UnitOfWork.SaveChangesAsync
```

**核心实现**：[ReservationAppService.CreateAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/ReservationAppService.cs#L41-L94)

**关键业务规则**（由 [ReservationDomainService.ValidateReservationAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/ReservationDomainService.cs#L38-L87) 执行）：

| 校验项 | 规则 | 实现位置 |
|--------|------|----------|
| 提前量 | `StartTime >= now + 1小时` | `MinAdvanceTime = TimeSpan.FromHours(1)` |
| 用户禁用 | `User.BanExpiryTime > now` 时禁止 | `User.IsBanned()` |
| 仪器类型 | 必须存在且 `Status = Enabled` | `InstrumentTypeAppService` 中校验 |
| 仪器可用性 | 仪器 `Status = Available` | `Instrument.CanBeReserved()` |
| 时间冲突 | 所选仪器在 `[StartTime, EndTime)` 内无其他活跃预约 | `GetOccupiedInstrumentIdsAsync` + `UPDLOCK/HOLDLOCK` |

**幂等性机制**：
- 请求体可选传 `IdempotencyKey`（客户端生成的唯一标识）
- 数据库 `Reservations.IdempotencyKey` 建有**过滤唯一索引**（`WHERE IdempotencyKey IS NOT NULL`）
- 创建前先查：若已存在同幂等键的预约，直接返回已有结果，不重复创建

**用户并发创建处理**：`GetOrCreateUserAsync` 中，若两个请求同时为新手机号创建用户，依赖 `Users.Phone` 唯一索引，一个插入成功，另一个捕获异常后重新查询返回已存在用户。

**冲突检测 SQL**（[InstrumentRepository.GetOccupiedInstrumentIdsAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Infrastructure/Data/Repositories/InstrumentRepositories.cs#L69-L91)）：

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

> `UPDLOCK` 申请更新锁，`HOLDLOCK` 保持锁到事务结束，配合 `Serializable` 隔离级别的键范围锁，确保并发下不会超卖。

---

#### 3.1.2 查询预约

**入口**：
- `GET /api/reservations/active?phone={phone}` — 当前有效预约
- `GET /api/reservations/history?phone={phone}` — 历史预约
- `GET /api/reservations/{id}` — 预约详情

**实现**：[ReservationAppService](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/ReservationAppService.cs#L120-L157)

**数据流向**：
```
Controller → ReservationAppService.GetActiveByPhoneAsync / GetHistoryByPhoneAsync
  → ReservationRepository.GetActiveByPhoneAsync / GetByPhoneAsync
    → EF Core Include(Items).ThenInclude(Instrument).Include(InstrumentType)
  → MapToResponse（实体 → ReservationResponse）
```

**有效预约判定**：`Status IN (Pending, InUse)`，按 `StartTime` 升序。
**历史预约**：该手机号全部预约，按 `CreatedAt` 降序。

**索引支撑**：`IX_Reservations_PhoneStatus(Phone, Status)` 加速按手机号+状态查询。

---

#### 3.1.3 取消预约

**入口**：`POST /api/reservations/{id}/cancel`

**请求体**：
```json
{
  "instrumentIds": []   // 空数组或不传 = 取消整单；传仪器ID = 部分取消
}
```

**实现**：[ReservationAppService.CancelAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/ReservationAppService.cs#L159-L180)

**领域逻辑**（[Reservation 实体](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Domain/Entities/Reservation.cs#L99-L126)）：

| 操作 | 行为 |
|------|------|
| 整单取消 `Cancel()` | 预约单状态 → `Cancelled`，所有明细项 → `Cancelled` |
| 部分取消 `CancelItems(ids)` | 指定仪器的明细项 → `Cancelled`；若所有明细均取消，预约单 → `Cancelled`，否则保持 `Pending` |

**前置校验**：仅 `Pending` 状态的预约可取消，否则抛 `ReservationCannotCancel`。

---

#### 3.1.4 违约判定与禁用

**触发方式**：定时任务 `DefaultHandlingHostedService` 每分钟执行一次。

**实现**：[DefaultHandlingService.ProcessDefaultsAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/DefaultHandlingService.cs#L41-L78)

**流程**：
```
1. 查询所有 Status=Pending 且 EndTime < now 的预约
   （GetPendingForDefaultCheckAsync）
2. 遍历每条预约：
   ├─ reservation.MarkDefaulted()  → 预约状态=Defaulted，Pending明细=Defaulted
   ├─ user.ApplyBan(24小时)         → User.BanExpiryTime = now + 24h
   └─ 创建 Notification(Type=DefaultBanned)
3. 统一 SaveChangesAsync
```

**违约判定规则**（[ReservationDomainService.IsDefaulted](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/ReservationDomainService.cs#L89-L93)）：
> `Status == Pending && EndTime < now` → 违约

**禁用逻辑**：`User.BanExpiryTime` 字段存储解禁时间，预约创建时 `User.IsBanned()` 判断 `BanExpiryTime > now` 则禁止预约。

**状态流转图**：

```
Pending ──(用户取消)──> Cancelled
Pending ──(定时任务检测 EndTime<now)──> Defaulted ──> User.BanExpiryTime = now+24h
Pending ──(实际使用完毕)──> Completed
```

---

### 3.2 管理员模块

#### 3.2.1 仪器类型管理

**入口**：
- `POST /api/instrument-types` — 新增
- `PUT /api/instrument-types/{id}` — 修改
- `POST /api/instrument-types/{id}/disable` — 禁用
- `GET /api/instrument-types` — 列表
- `GET /api/instrument-types/{id}` — 详情

**实现**：[InstrumentTypeAppService](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/InstrumentTypeAppService.cs)

**核心逻辑**：
- 新增：校验 `Code` 唯一（`IX_InstrumentTypes_Code`），创建 `InstrumentType` 实体
- 修改：仅允许修改 `Name` 和 `Description`
- 禁用：`Status → Disabled`，禁用后该类型下仪器不可被新预约（预约校验时检查类型状态）

**实体行为**（[InstrumentType](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Domain/Entities/InstrumentType.cs)）：
- `Update(name, description)` — 更新名称与描述
- `Disable()` / `Enable()` — 状态切换

---

#### 3.2.2 仪器管理

**入口**：
- `POST /api/instruments` — 新增
- `PUT /api/instruments/{id}` — 修改
- `GET /api/instruments/{id}` — 详情
- `GET /api/instruments?instrumentTypeId={id}` — 按类型列表

**实现**：[InstrumentAppService](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/InstrumentAppService.cs#L39-L68)

**核心逻辑**：
- 新增：校验所属类型存在 + `Code` 唯一，创建 `Instrument`（初始 `Status = Available`）
- 修改：仅允许修改 `Name`

---

#### 3.2.3 仪器状态变更（故障/报废/恢复）

**入口**：
- `POST /api/instruments/{id}/fault` — 标记故障
- `POST /api/instruments/{id}/scrap` — 标记报废
- `POST /api/instruments/{id}/recover` — 恢复故障

**实现**：[InstrumentAppService.MarkFaultAsync / MarkScrappedAsync / RecoverAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/InstrumentAppService.cs#L73-L138)

**故障处理流程**：
```
1. instrument.MarkFault()  → Status = Faulty
2. 查询该仪器的所有有效预约（GetActiveByInstrumentAsync）
3. 为每条预约创建 Notification(Type=InstrumentFault)
4. SaveChanges（状态变更 + 通知创建在同一事务内）
```

**报废处理流程**：
```
1. instrument.MarkScrapped()  → Status = Scrapped
2. 查询该仪器的所有有效预约
3. 对每条预约执行 reservation.CancelItems([instrumentId])
   → 该仪器的预约项 → Cancelled
   → 若预约单所有明细均取消，预约单 → Cancelled
4. 为每条预约创建 Notification(Type=InstrumentScrapped)
5. SaveChanges
```

**恢复流程**：`instrument.Recover()` → `Status = Available`（仅故障仪器可恢复，报废不可恢复）。

**实体行为**（[Instrument](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Domain/Entities/Instrument.cs)）：
- `MarkFault()` / `MarkScrapped()` / `Recover()` — 状态机
- `CanBeReserved()` — `Status == Available` 时可预约

**数据一致性保证**：仪器状态变更 + 受影响预约处理 + 通知创建全部在同一个 `SaveChanges` 中提交，要么全部成功要么全部回滚。

---

#### 3.2.4 资源使用情况查询

**入口**：`GET /api/instruments/usage?instrumentTypeId={id}`（可选）

**实现**：[InstrumentAppService.GetUsageAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/InstrumentAppService.cs#L159-L189)

**返回数据**：每台仪器的当前状态、有效预约数量、即将到来的预约时段列表（含预约人手机号）。

---

### 3.3 通知模块

#### 3.3.1 通知记录存储

所有通知均持久化到 `Notifications` 表，包含：
- `Type`：预约创建/取消、仪器故障、仪器报废、违约禁用
- `Content`：通知内容
- `Status`：Pending / Sent / Failed
- `RetryCount`：重试次数

#### 3.3.2 通知分发定时任务

**实现**：`NotificationDispatchHostedService`（每 30 秒执行一次）→ [NotificationDispatchService.DispatchPendingAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Services/NotificationDispatchService.cs#L41-L72)

**流程**：
```
1. 查询 Status=Pending 且 RetryCount < 3 的通知（最多50条）
2. 逐条调用 INotificationSender.SendAsync
   ├─ 成功 → MarkSent()
   └─ 失败/异常 → MarkFailed()（RetryCount++）
3. SaveChanges
```

**通知发送器**：当前为 [MockNotificationSender](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Infrastructure/Services/MockNotificationSender.cs)，仅记录日志。生产环境可实现 `INotificationSender` 接口对接企业微信/短信/邮件网关。

**失败重试**：`RetryCount` 达到 3 后不再重试，可人工介入。

---

### 3.4 定时任务模块

系统包含两个 `IHostedService` 后台服务，在 [DependencyInjection](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Infrastructure/DependencyInjection.cs) 中注册：

| 服务 | 间隔 | 职责 |
|------|------|------|
| `NotificationDispatchHostedService` | 30 秒 | 扫描并发送待处理通知 |
| `DefaultHandlingHostedService` | 1 分钟 | 扫描并处理违约预约 |

**设计要点**：
- 通过 `IServiceProvider.CreateScope()` 创建作用域，解决 `IHostedService` 单例与 Scoped 仓储的生命周期冲突
- 全局 try-catch 确保单轮异常不影响后续调度
- `CancellationToken` 支持优雅停机

---

## 四、数据库设计

### 4.1 表结构

#### Users（用户/实验人员）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | uniqueidentifier | 主键 |
| Phone | nvarchar(20) | 手机号，唯一索引 `IX_Users_Phone` |
| Name | nvarchar(50) | 姓名（可空） |
| BanExpiryTime | datetime2 | 违约禁用到期时间（UTC） |
| CreatedAt/UpdatedAt | datetime2 | 时间戳，默认 GETUTCDATE() |

#### InstrumentTypes（仪器类型）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | uniqueidentifier | 主键 |
| Name | nvarchar(100) | 类型名称 |
| Code | nvarchar(50) | 类型编码，唯一索引 `IX_InstrumentTypes_Code` |
| Description | nvarchar(500) | 描述 |
| Status | int | 1=Enabled, 2=Disabled |

#### Instruments（仪器）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | uniqueidentifier | 主键 |
| InstrumentTypeId | uniqueidentifier | 外键 → InstrumentTypes |
| Name | nvarchar(100) | 仪器名称 |
| Code | nvarchar(50) | 仪器编码，唯一索引 `IX_Instruments_Code` |
| Status | int | 1=Available, 2=Faulty, 3=Scrapped |
| RowVersion | rowversion | 乐观并发令牌 |
| 索引 | `IX_Instruments_TypeStatus(InstrumentTypeId, Status)` | 按类型+状态查询 |

#### Reservations（预约单）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | uniqueidentifier | 主键 |
| UserId | uniqueidentifier | 外键 → Users |
| Phone | nvarchar(20) | 冗余手机号，便于按手机号查询 |
| InstrumentTypeId | uniqueidentifier | 外键 → InstrumentTypes |
| StartTime/EndTime | datetime2 | 预约时段（UTC） |
| Status | int | 1=Pending, 2=InUse, 3=Completed, 4=Cancelled, 5=Defaulted |
| Remark | nvarchar(500) | 备注 |
| IdempotencyKey | nvarchar(100) | 幂等键，过滤唯一索引 |
| RowVersion | rowversion | 乐观并发令牌 |
| 索引 | `IX_Reservations_TypeStatusTime(InstrumentTypeId, Status, StartTime, EndTime)` | 冲突检测核心索引 |
| 索引 | `IX_Reservations_PhoneStatus(Phone, Status)` | 按手机号查询 |
| 索引 | `IX_Reservations_IdempotencyKey` 唯一（过滤NULL） | 幂等防重 |

#### ReservationItems（预约明细）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | uniqueidentifier | 主键 |
| ReservationId | uniqueidentifier | 外键 → Reservations（级联删除） |
| InstrumentId | uniqueidentifier | 外键 → Instruments |
| Status | int | 1=Pending, 2=Completed, 3=Cancelled, 4=Defaulted |
| 索引 | `IX_ReservationItems_InstrumentStatus(InstrumentId, Status)` | 按仪器查有效预约 |

#### Notifications（通知记录）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | uniqueidentifier | 主键 |
| UserId | uniqueidentifier | 外键 → Users |
| ReservationId | uniqueidentifier | 外键 → Reservations（可空，SET NULL） |
| Type | int | 通知类型枚举 |
| Content | nvarchar(1000) | 通知内容 |
| Status | int | 1=Pending, 2=Sent, 3=Failed |
| SentAt | datetime2 | 发送时间 |
| RetryCount | int | 重试次数 |
| 索引 | `IX_Notifications_StatusRetry(Status, RetryCount)` | 定时任务扫描 |

### 4.2 实体关系图

```
Users (1) ────< (N) Reservations (N) >──── (1) InstrumentTypes
                     │
                     │ (1)
                     │
                     ▼
              ReservationItems (N) >──── (1) Instruments
                     │
Users (1) ────< (N) Notifications (N) >──── (0..1) Reservations
```

---

## 五、并发控制机制

### 5.1 预约防超卖（核心）

采用 **Serializable 事务 + UPDLOCK/HOLDLOCK 悲观锁** 组合策略：

```
┌──────────────────────────────────────────────────────────┐
│  预约创建事务（Serializable 隔离级别）                    │
│                                                           │
│  ① BEGIN TRANSACTION (Serializable)                       │
│  ② SELECT ... WITH (UPDLOCK, HOLDLOCK)  ← 冲突检测         │
│     └─ 锁定 ReservationItems + Reservations 相关行         │
│        及索引范围，阻止其他事务插入冲突预约                 │
│  ③ INSERT Reservation + ReservationItems                  │
│  ④ COMMIT TRANSACTION                                     │
└──────────────────────────────────────────────────────────┘
```

**实现位置**：
- 事务开启：[UnitOfWork.BeginTransactionAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Infrastructure/Data/UnitOfWork.cs#L21-L28)（`IsolationLevel.Serializable`）
- 锁提示：[InstrumentRepository.GetOccupiedInstrumentIdsAsync](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Infrastructure/Data/Repositories/InstrumentRepositories.cs#L77-L85)

**为什么有效**：
- `Serializable` 隔离级别启用键范围锁（Key-Range Locking）
- `UPDLOCK` 将共享锁升级为更新锁，防止两个事务同时读到"无冲突"后都执行插入
- `HOLDLOCK` 保持锁到事务结束

### 5.2 乐观并发

`Instrument` 和 `Reservation` 表使用 `rowversion` 类型的 `RowVersion` 字段：
- EF Core 配置 `.IsRowVersion()`
- 更新时 EF Core 自动在 WHERE 子句中携带原始 RowVersion
- 若并发修改导致 RowVersion 不匹配，抛出 `DbUpdateConcurrencyException`

### 5.3 幂等防重

- `Reservations.IdempotencyKey` 过滤唯一索引
- 应用层先查后插，重复请求返回已有结果

---

## 六、API 接口设计

### 6.1 统一响应格式

```json
{
  "code": "0",
  "message": "success",
  "data": { ... }
}
```

实现：[ApiResponse](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Common/ApiResponse.cs)

### 6.2 实验人员接口

| 方法 | 路径 | 说明 | 请求体/参数 |
|------|------|------|-------------|
| POST | `/api/reservations` | 创建预约 | `CreateReservationRequest` |
| GET | `/api/reservations/{id}` | 预约详情 | path: id |
| GET | `/api/reservations/active` | 有效预约 | query: phone |
| GET | `/api/reservations/history` | 历史预约 | query: phone |
| POST | `/api/reservations/{id}/cancel` | 取消预约 | `CancelReservationRequest` |

### 6.3 管理员接口

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

### 6.4 错误码定义

| 码 | 说明 | 触发场景 |
|----|------|----------|
| 0 | 成功 | - |
| 1001 | 参数错误 | 请求模型校验失败 |
| 1002 | 资源不存在 | 通用 |
| 2001 | 预约需提前1小时 | StartTime < now+1h |
| 2002 | 用户违约禁用中 | BanExpiryTime > now |
| 2003 | 仪器类型已禁用 | Type.Status = Disabled |
| 2004 | 仪器不可用 | Instrument.Status ≠ Available |
| 2005 | 仪器时间段冲突 | 冲突检测命中 |
| 2006 | 预约单不存在 | 查询为空 |
| 2007 | 预约不可取消 | Status ≠ Pending |
| 2008 | 幂等键重复 | （实际直接返回已有结果） |
| 3001 | 仪器类型不存在 | - |
| 3002 | 仪器不存在 | - |
| 3003 | 仪器类型编码已存在 | Code 冲突 |
| 3004 | 仪器编码已存在 | Code 冲突 |

完整定义：[ErrorCodes.cs](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Common/ErrorCodes.cs)

---

## 七、异常处理

### 7.1 全局异常中间件

[GlobalExceptionMiddleware](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Api/Middleware/GlobalExceptionMiddleware.cs) 注册为最外层中间件：

```
请求 → GlobalExceptionMiddleware → Controller → ...
         │
         ├─ BusinessException → 400 + { code, message }
         └─ Exception         → 500 + { code: "9999", message: "服务器内部错误" }
```

### 7.2 业务异常

[BusinessException](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/Common/BusinessException.cs) 携带 `Code` + `Message`，由应用服务在校验失败时抛出。

---

## 八、依赖注入与生命周期

### 8.1 注册位置

- Application 层服务：[Application.DependencyInjection](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Application/DependencyInjection.cs)
- Infrastructure 层服务：[Infrastructure.DependencyInjection](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Infrastructure/DependencyInjection.cs)
- API 入口：[Program.cs](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Api/Program.cs) 调用 `AddApplication()` + `AddInfrastructure()`

### 8.2 生命周期

| 服务 | 生命周期 | 说明 |
|------|----------|------|
| AppDbContext | Scoped | EF Core 默认 |
| UnitOfWork | Scoped | 与 DbContext 同生命周期 |
| 所有 Repository | Scoped | - |
| 所有 AppService | Scoped | - |
| ReservationDomainService | Scoped | - |
| INotificationSender | Singleton | Mock 无状态 |
| HostedServices | Singleton | 后台服务单例 |

---

## 九、数据库连接与迁移

### 9.1 连接字符串

配置于 [appsettings.json](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Api/appsettings.json)：

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=AgilentQuiz;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Encrypt=False;"
}
```

### 9.2 连接弹性

EF Core 配置 `EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: 30s)`，应对 SQL Server 瞬态故障自动重试。

### 9.3 自动迁移

开发环境下 [Program.cs](file:///Users/michael/Documents/trae_projects/Agilent%20Quiz/src/AgilentQuiz.Api/Program.cs#L33-L45) 启动时执行 `dbContext.Database.Migrate()`，失败仅记日志不阻断启动。

---

## 十、测试策略

### 10.1 测试结构

| 测试文件 | 覆盖范围 | 用例数 |
|----------|----------|--------|
| `ReservationEntityTests` | 预约取消（整单/部分）、违约标记 | 4 |
| `UserEntityTests` | 违约禁用判定 | 4 |
| `ReservationDomainServiceTests` | 提前量/禁用/可用性/冲突校验、违约判定 | 7 |
| `ReservationAppServiceTests` | 幂等返回、类型不存在、冲突异常、成功创建、取消校验 | 7 |

**总计：22 个用例，全部通过。**

### 10.2 测试技术

- **xUnit** 测试框架
- **Moq** 模拟仓储与领域服务
- **NullLogger** 避免日志依赖

### 10.3 运行

```bash
dotnet test
```

---

## 十一、运行部署

### 11.1 启动 SQL Server

```bash
docker-compose up -d
```

### 11.2 初始化数据

```bash
# 方式一：API 启动自动迁移（开发环境）
dotnet run --project src/AgilentQuiz.Api

# 方式二：手动执行脚本
sqlcmd -S localhost,1433 -U sa -P YourStrong@Passw0rd -d AgilentQuiz -i database/init.sql
sqlcmd -S localhost,1433 -U sa -P YourStrong@Passw0rd -d AgilentQuiz -i database/seed.sql
```

### 11.3 启动 API

```bash
dotnet run --project src/AgilentQuiz.Api
```

Swagger UI：`https://localhost:5001/swagger`

---

## 十二、关键设计决策说明

| 决策 | 选择 | 原因 |
|------|------|------|
| ORM | EF Core | 题目要求，迁移管理方便，Linq 查询可读性好 |
| 冲突检测锁 | Serializable + UPDLOCK | SQL Server 原生方案，无需引入 Redis，强一致防超卖 |
| 幂等实现 | 幂等键 + 唯一索引 | 数据库层兜底，应用层快路径返回 |
| 通知 | 表存储 + 定时分发 | 失败可重试，支持审计，解耦业务与发送 |
| 违约处理 | 定时任务扫描 | 简单可靠，无需消息队列，满足当前规模 |
| 状态管理 | 枚举 int 存储 | 性能优于字符串，EF Core HasConversion |
| 时间存储 | UTC | 避免时区混乱，前端按需转换 |
