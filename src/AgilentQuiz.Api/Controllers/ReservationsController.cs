using AgilentQuiz.Application.Common;
using AgilentQuiz.Application.DTOs.Reservation;
using AgilentQuiz.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgilentQuiz.Api.Controllers;

/// <summary>
/// 预约管理（实验人员接口）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationAppService _reservationService;

    public ReservationsController(IReservationAppService reservationService)
    {
        _reservationService = reservationService;
    }

    /// <summary>
    /// 创建预约
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<ReservationResponse>.Ok(result));
    }

    /// <summary>
    /// 获取预约详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ReservationResponse>.Ok(result));
    }

    /// <summary>
    /// 查询当前有效预约（按手机号）
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReservationResponse>>>> GetActive(
        [FromQuery] string phone,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.GetActiveByPhoneAsync(phone, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReservationResponse>>.Ok(result));
    }

    /// <summary>
    /// 查询历史预约（按手机号）
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReservationResponse>>>> GetHistory(
        [FromQuery] string phone,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.GetHistoryByPhoneAsync(phone, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReservationResponse>>.Ok(result));
    }

    /// <summary>
    /// 取消预约（整个预约单或部分仪器）
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Cancel(
        Guid id,
        [FromBody] CancelReservationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.CancelAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ReservationResponse>.Ok(result));
    }
}
