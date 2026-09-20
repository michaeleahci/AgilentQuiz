using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Domain.Entities;

/// <summary>
/// 通知记录
/// </summary>
public class Notification : Entity
{
    /// <summary>
    /// 接收用户ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// 关联预约单ID（可为空）
    /// </summary>
    public Guid? ReservationId { get; private set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// 通知内容
    /// </summary>
    public string Content { get; private set; } = null!;

    /// <summary>
    /// 发送状态
    /// </summary>
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; private set; }

    public User User { get; private set; } = null!;
    public Reservation? Reservation { get; private set; }

    private Notification() { }

    public Notification(
        Guid userId,
        NotificationType type,
        string content,
        Guid? reservationId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID无效", nameof(userId));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("通知内容不能为空", nameof(content));

        UserId = userId;
        Type = type;
        Content = content;
        ReservationId = reservationId;
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = NotificationStatus.Failed;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetForRetry()
    {
        Status = NotificationStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }
}
