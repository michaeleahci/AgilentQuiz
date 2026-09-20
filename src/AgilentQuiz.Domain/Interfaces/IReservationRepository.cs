using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Domain.Interfaces;

/// <summary>
/// 预约仓储接口
/// </summary>
public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetActiveByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<Reservation?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
    void Update(Reservation reservation);

    /// <summary>
    /// 获取某仪器的有效预约（用于故障/报废时处理受影响预约）
    /// </summary>
    Task<IReadOnlyList<Reservation>> GetActiveByInstrumentAsync(Guid instrumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取需要进行违约判定的预约（状态为 Pending 且结束时间已过）
    /// </summary>
    Task<IReadOnlyList<Reservation>> GetPendingForDefaultCheckAsync(DateTime now, CancellationToken cancellationToken = default);
}
