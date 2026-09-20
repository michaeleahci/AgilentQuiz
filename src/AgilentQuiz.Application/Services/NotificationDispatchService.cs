using AgilentQuiz.Domain.Interfaces;
using AgilentQuiz.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AgilentQuiz.Application.Services;

/// <summary>
/// 通知分发服务：扫描待发送通知并调用 INotificationSender 发送
/// </summary>
public interface INotificationDispatchService
{
    /// <summary>
    /// 处理一批待发送通知
    /// </summary>
    Task DispatchPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 通知分发服务实现
/// </summary>
public class NotificationDispatchService : INotificationDispatchService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDispatchService> _logger;
    private const int MaxRetryCount = 3;

    public NotificationDispatchService(
        INotificationRepository notificationRepository,
        INotificationSender notificationSender,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDispatchService> logger)
    {
        _notificationRepository = notificationRepository;
        _notificationSender = notificationSender;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepository.GetPendingNotificationsAsync(MaxRetryCount, cancellationToken);
        if (notifications.Count == 0) return;

        _logger.LogInformation("Dispatching {Count} pending notifications", notifications.Count);

        foreach (var notification in notifications)
        {
            try
            {
                var success = await _notificationSender.SendAsync(notification, cancellationToken);
                if (success)
                {
                    notification.MarkSent();
                }
                else
                {
                    notification.MarkFailed();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification {Id}", notification.Id);
                notification.MarkFailed();
            }

            _notificationRepository.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
