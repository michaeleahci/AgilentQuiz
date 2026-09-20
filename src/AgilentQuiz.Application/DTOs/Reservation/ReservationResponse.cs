using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Application.DTOs.Reservation;

/// <summary>
/// 预约明细响应
/// </summary>
public class ReservationItemResponse
{
    public Guid Id { get; set; }
    public Guid InstrumentId { get; set; }
    public string InstrumentName { get; set; } = null!;
    public string InstrumentCode { get; set; } = null!;
    public ReservationItemStatus Status { get; set; }
}

/// <summary>
/// 预约单响应
/// </summary>
public class ReservationResponse
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = null!;
    public Guid InstrumentTypeId { get; set; }
    public string InstrumentTypeName { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ReservationStatus Status { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ReservationItemResponse> Items { get; set; } = new();
}
