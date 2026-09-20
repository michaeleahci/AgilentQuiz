using System.ComponentModel.DataAnnotations;

namespace AgilentQuiz.Application.DTOs.Reservation;

/// <summary>
/// 创建预约请求
/// </summary>
public class CreateReservationRequest
{
    /// <summary>
    /// 联系手机号
    /// </summary>
    [Required]
    public string Phone { get; set; } = null!;

    /// <summary>
    /// 预约开始时间（UTC）
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 预约结束时间（UTC）
    /// </summary>
    [Required]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 仪器类型ID
    /// </summary>
    [Required]
    public Guid InstrumentTypeId { get; set; }

    /// <summary>
    /// 本次预约的仪器ID列表（同一类型下的多台）
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<Guid> InstrumentIds { get; set; } = new();

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 幂等键（客户端生成，用于防重复提交）
    /// </summary>
    public string? IdempotencyKey { get; set; }
}
