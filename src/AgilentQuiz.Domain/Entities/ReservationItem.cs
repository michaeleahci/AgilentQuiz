using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Domain.Entities;

/// <summary>
/// 预约明细项（对应预约单中的单台仪器）
/// </summary>
public class ReservationItem : Entity
{
    /// <summary>
    /// 所属预约单ID
    /// </summary>
    public Guid ReservationId { get; private set; }

    /// <summary>
    /// 仪器ID
    /// </summary>
    public Guid InstrumentId { get; private set; }

    /// <summary>
    /// 明细状态
    /// </summary>
    public ReservationItemStatus Status { get; private set; } = ReservationItemStatus.Pending;

    public Reservation Reservation { get; private set; } = null!;
    public Instrument Instrument { get; private set; } = null!;

    private ReservationItem() { }

    public ReservationItem(Guid reservationId, Guid instrumentId)
    {
        if (reservationId == Guid.Empty)
            throw new ArgumentException("预约单ID无效", nameof(reservationId));
        if (instrumentId == Guid.Empty)
            throw new ArgumentException("仪器ID无效", nameof(instrumentId));

        ReservationId = reservationId;
        InstrumentId = instrumentId;
    }

    public void Cancel()
    {
        Status = ReservationItemStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDefaulted()
    {
        Status = ReservationItemStatus.Defaulted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = ReservationItemStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
}
