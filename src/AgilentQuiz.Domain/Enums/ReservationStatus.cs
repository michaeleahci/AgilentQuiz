namespace AgilentQuiz.Domain.Enums;

/// <summary>
/// 预约单状态
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// 待使用（已预约未到使用时间）
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 使用中
    /// </summary>
    InUse = 2,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// 违约（未在规定时间内取消且未实际使用）
    /// </summary>
    Defaulted = 5
}
