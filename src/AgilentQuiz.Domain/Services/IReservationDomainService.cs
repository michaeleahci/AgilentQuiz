using AgilentQuiz.Domain.Entities;

namespace AgilentQuiz.Domain.Services;

/// <summary>
/// 预约领域服务：封装预约相关的核心业务规则
/// </summary>
public interface IReservationDomainService
{
    /// <summary>
    /// 校验预约是否满足业务规则（提前1小时、用户未被禁用、仪器可用、时间段无冲突）
    /// </summary>
    /// <param name="reservation">待创建的预约单</param>
    /// <param name="instrumentIds">本次预约的仪器ID列表</param>
    /// <param name="cancellationToken"></param>
    /// <returns>校验通过返回 null，否则返回错误信息</returns>
    Task<string?> ValidateReservationAsync(
        Reservation reservation,
        IReadOnlyList<Guid> instrumentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 判定预约是否违约（结束时间已过且未取消，视为违约）
    /// </summary>
    bool IsDefaulted(Reservation reservation, DateTime now);
}
