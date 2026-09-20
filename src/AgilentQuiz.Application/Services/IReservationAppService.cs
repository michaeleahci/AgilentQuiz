using AgilentQuiz.Application.DTOs.Reservation;

namespace AgilentQuiz.Application.Services;

/// <summary>
/// 预约应用服务接口
/// </summary>
public interface IReservationAppService
{
    /// <summary>
    /// 创建预约（支持幂等键）
    /// </summary>
    Task<ReservationResponse> CreateAsync(CreateReservationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据手机号查询当前有效预约
    /// </summary>
    Task<IReadOnlyList<ReservationResponse>> GetActiveByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据手机号查询历史预约
    /// </summary>
    Task<IReadOnlyList<ReservationResponse>> GetHistoryByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取预约详情
    /// </summary>
    Task<ReservationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消预约（整个预约单或部分仪器）
    /// </summary>
    Task<ReservationResponse> CancelAsync(Guid reservationId, CancelReservationRequest request, CancellationToken cancellationToken = default);
}
