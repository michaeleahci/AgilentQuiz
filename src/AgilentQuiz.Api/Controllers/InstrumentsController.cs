using AgilentQuiz.Application.Common;
using AgilentQuiz.Application.DTOs.Instrument;
using AgilentQuiz.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgilentQuiz.Api.Controllers;

/// <summary>
/// 仪器管理（管理员接口）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InstrumentsController : ControllerBase
{
    private readonly IInstrumentAppService _service;

    public InstrumentsController(IInstrumentAppService service) => _service = service;

    /// <summary>
    /// 新增仪器
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<InstrumentResponse>>> Create(
        [FromBody] CreateInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<InstrumentResponse>.Ok(result));
    }

    /// <summary>
    /// 修改仪器
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<InstrumentResponse>>> Update(
        Guid id,
        [FromBody] UpdateInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InstrumentResponse>.Ok(result));
    }

    /// <summary>
    /// 标记故障
    /// </summary>
    [HttpPost("{id}/fault")]
    public async Task<ActionResult<ApiResponse>> MarkFault(Guid id, CancellationToken cancellationToken)
    {
        await _service.MarkFaultAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// 标记报废
    /// </summary>
    [HttpPost("{id}/scrap")]
    public async Task<ActionResult<ApiResponse>> MarkScrapped(Guid id, CancellationToken cancellationToken)
    {
        await _service.MarkScrappedAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// 恢复故障仪器
    /// </summary>
    [HttpPost("{id}/recover")]
    public async Task<ActionResult<ApiResponse>> Recover(Guid id, CancellationToken cancellationToken)
    {
        await _service.RecoverAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// 获取仪器详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InstrumentResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<InstrumentResponse>.Ok(result));
    }

    /// <summary>
    /// 按类型获取仪器列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InstrumentResponse>>>> GetByType(
        [FromQuery] Guid instrumentTypeId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByTypeAsync(instrumentTypeId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InstrumentResponse>>.Ok(result));
    }

    /// <summary>
    /// 查看资源使用情况
    /// </summary>
    [HttpGet("usage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InstrumentUsageResponse>>>> GetUsage(
        [FromQuery] Guid? instrumentTypeId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetUsageAsync(instrumentTypeId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InstrumentUsageResponse>>.Ok(result));
    }
}
