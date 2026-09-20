namespace AgilentQuiz.Domain.Enums;

/// <summary>
/// 通知类型
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 预约创建成功
    /// </summary>
    ReservationCreated = 1,

    /// <summary>
    /// 预约取消
    /// </summary>
    ReservationCancelled = 2,

    /// <summary>
    /// 仪器故障
    /// </summary>
    InstrumentFault = 3,

    /// <summary>
    /// 仪器报废
    /// </summary>
    InstrumentScrapped = 4,

    /// <summary>
    /// 违约禁用
    /// </summary>
    DefaultBanned = 5
}
