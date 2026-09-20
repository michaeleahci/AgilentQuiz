using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;
using AgilentQuiz.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgilentQuiz.Application.Services;

/// <summary>
/// 违约处理服务：定时扫描已结束但未取消的预约，标记为违约并禁用用户24小时
/// </summary>
public interface IDefaultHandlingService
{
    Task ProcessDefaultsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 违约处理服务实现
/// </summary>
public class DefaultHandlingService : IDefaultHandlingService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DefaultHandlingService> _logger;

    public DefaultHandlingService(
        IReservationRepository reservationRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<DefaultHandlingService> logger)
    {
        _reservationRepository = reservationRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pendingReservations = await _reservationRepository.GetPendingForDefaultCheckAsync(now, cancellationToken);

        if (pendingReservations.Count == 0) return;

        _logger.LogInformation("Found {Count} reservations to check for default", pendingReservations.Count);

        foreach (var reservation in pendingReservations)
        {
            // 仅当预约结束时间已过且仍为 Pending 状态时，判定为违约
            if (reservation.Status != ReservationStatus.Pending || reservation.EndTime >= now)
                continue;

            reservation.MarkDefaulted();
            _reservationRepository.Update(reservation);

            // 禁用用户 24 小时
            var user = await _userRepository.GetByIdAsync(reservation.UserId, cancellationToken);
            if (user != null)
            {
                user.ApplyBan(ReservationDomainService.DefaultBanDuration);
                _userRepository.Update(user);

                // 发送违约禁用通知
                var notification = new Notification(
                    user.Id,
                    NotificationType.DefaultBanned,
                    $"您因未按时使用预约仪器且未及时取消，已被判定违约，将被禁止预约 24 小时，解禁时间: {user.BanExpiryTime:yyyy-MM-dd HH:mm:ss} UTC。",
                    reservation.Id);
                await _notificationRepository.AddAsync(notification, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Default handling completed");
    }
}
