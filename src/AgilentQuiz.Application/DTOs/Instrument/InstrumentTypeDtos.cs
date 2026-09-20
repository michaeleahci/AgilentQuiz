using System.ComponentModel.DataAnnotations;

namespace AgilentQuiz.Application.DTOs.Instrument;

/// <summary>
/// 新增仪器类型请求
/// </summary>
public class CreateInstrumentTypeRequest
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;

    public string? Description { get; set; }
}

/// <summary>
/// 修改仪器类型请求
/// </summary>
public class UpdateInstrumentTypeRequest
{
    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}

/// <summary>
/// 仪器类型响应
/// </summary>
public class InstrumentTypeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public Domain.Enums.InstrumentTypeStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
