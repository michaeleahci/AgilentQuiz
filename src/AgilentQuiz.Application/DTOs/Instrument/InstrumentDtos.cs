using System.ComponentModel.DataAnnotations;
using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Application.DTOs.Instrument;

/// <summary>
/// 新增仪器请求
/// </summary>
public class CreateInstrumentRequest
{
    [Required]
    public Guid InstrumentTypeId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;
}

/// <summary>
/// 修改仪器请求
/// </summary>
public class UpdateInstrumentRequest
{
    [Required]
    public string Name { get; set; } = null!;
}

/// <summary>
/// 仪器响应
/// </summary>
public class InstrumentResponse
{
    public Guid Id { get; set; }
    public Guid InstrumentTypeId { get; set; }
    public string InstrumentTypeName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public InstrumentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 仪器资源使用情况响应
/// </summary>
public class InstrumentUsageResponse
{
    public Guid InstrumentId { get; set; }
    public string InstrumentName { get; set; } = null!;
    public string InstrumentCode { get; set; } = null!;
    public InstrumentStatus Status { get; set; }
    public int ActiveReservationCount { get; set; }
    public List<ReservationSlot> UpcomingSlots { get; set; } = new();
}

public class ReservationSlot
{
    public Guid ReservationId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Phone { get; set; } = null!;
}
