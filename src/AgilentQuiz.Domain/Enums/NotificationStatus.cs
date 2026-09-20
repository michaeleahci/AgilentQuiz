namespace AgilentQuiz.Domain.Enums;

/// <summary>
/// 通知发送状态
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// 待发送
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 已发送
    /// </summary>
    Sent = 2,

    /// <summary>
    /// 发送失败
    /// </summary>
    Failed = 3
}
