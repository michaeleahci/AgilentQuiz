namespace AgilentQuiz.Application.DTOs.Reservation;

/// <summary>
/// 取消预约请求
/// </summary>
public class CancelReservationRequest
{
    /// <summary>
    /// 要取消的仪器ID列表。为空或不传则取消整个预约单。
    /// </summary>
    public List<Guid>? InstrumentIds { get; set; }
}
