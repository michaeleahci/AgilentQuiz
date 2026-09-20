using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;
using AgilentQuiz.Domain.Interfaces;
using AgilentQuiz.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AgilentQuiz.Application.Services;

/// <summary>
/// 预约领域服务实现
/// </summary>
public class ReservationDomainService : IReservationDomainService
{
    private readonly IUserRepository _userRepository;
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly ILogger<ReservationDomainService> _logger;

    /// <summary>
    /// 最少提前预约时间
    /// </summary>
    private static readonly TimeSpan MinAdvanceTime = TimeSpan.FromHours(1);

    /// <summary>
    /// 违约禁用时长
    /// </summary>
    public static readonly TimeSpan DefaultBanDuration = TimeSpan.FromHours(24);

    public ReservationDomainService(
        IUserRepository userRepository,
        IInstrumentRepository instrumentRepository,
        ILogger<ReservationDomainService> logger)
    {
        _userRepository = userRepository;
        _instrumentRepository = instrumentRepository;
        _logger = logger;
    }

    public async Task<string?> ValidateReservationAsync(
        Reservation reservation,
        IReadOnlyList<Guid> instrumentIds,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // 1. 提前量校验：必须至少提前1小时
        if (reservation.StartTime < now.Add(MinAdvanceTime))
        {
            return $"预约必须至少提前 {MinAdvanceTime.TotalHours} 小时";
        }

        // 2. 用户禁用校验
        var user = await _userRepository.GetByIdAsync(reservation.UserId, cancellationToken);
        if (user != null && user.IsBanned())
        {
            return $"用户处于违约禁用期，禁用至 {user.BanExpiryTime:yyyy-MM-dd HH:mm:ss} UTC";
        }

        // 3. 仪器可用性校验
        var instruments = await _instrumentRepository.GetByIdsAsync(instrumentIds, cancellationToken);
        if (instruments.Count != instrumentIds.Count)
        {
            var missing = instrumentIds.Except(instruments.Select(i => i.Id)).ToList();
            return $"以下仪器不存在: {string.Join(", ", missing)}";
        }

        var unavailable = instruments.Where(i => !i.CanBeReserved()).ToList();
        if (unavailable.Count > 0)
        {
            return $"以下仪器不可用（故障或报废）: {string.Join(", ", unavailable.Select(i => i.Code))}";
        }

        // 4. 时间段冲突校验（同一仪器在该时间段内不能有其他有效预约）
        var occupiedIds = await _instrumentRepository.GetOccupiedInstrumentIdsAsync(
            reservation.InstrumentTypeId,
            reservation.StartTime,
            reservation.EndTime,
            cancellationToken);

        var conflictIds = instrumentIds.Intersect(occupiedIds).ToList();
        if (conflictIds.Count > 0)
        {
            var conflictInstruments = instruments.Where(i => conflictIds.Contains(i.Id)).ToList();
            return $"以下仪器在所选时间段内已被预约: {string.Join(", ", conflictInstruments.Select(i => i.Code))}";
        }

        return null;
    }

    public bool IsDefaulted(Reservation reservation, DateTime now)
    {
        // 预约结束时间已过 且 状态仍为 Pending（未取消、未标记完成），视为违约
        return reservation.Status == ReservationStatus.Pending && reservation.EndTime < now;
    }
}
