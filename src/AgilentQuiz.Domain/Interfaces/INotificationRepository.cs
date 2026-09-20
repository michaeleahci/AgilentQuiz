using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Domain.Interfaces;

/// <summary>
/// 通知仓储接口
/// </summary>
public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    void Update(Notification notification);
    Task<IReadOnlyList<Notification>> GetPendingNotificationsAsync(int maxRetryCount, CancellationToken cancellationToken = default);
}
