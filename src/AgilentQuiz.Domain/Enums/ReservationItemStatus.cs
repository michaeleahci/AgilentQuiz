namespace AgilentQuiz.Domain.Enums;

/// <summary>
/// 预约项状态（对应单台仪器的预约明细）
/// </summary>
public enum ReservationItemStatus
{
    /// <summary>
    /// 待使用
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// 违约
    /// </summary>
    Defaulted = 4
}
