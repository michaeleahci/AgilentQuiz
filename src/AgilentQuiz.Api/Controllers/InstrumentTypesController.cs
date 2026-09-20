using AgilentQuiz.Application.Common;
using AgilentQuiz.Application.DTOs.Instrument;
using AgilentQuiz.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgilentQuiz.Api.Controllers;

/// <summary>
/// 仪器类型管理（管理员接口）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InstrumentTypesController : ControllerBase
{
    private readonly IInstrumentTypeAppService _service;

    public InstrumentTypesController(IInstrumentTypeAppService service) => _service = service;

    /// <summary>
    /// 新增仪器类型
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<InstrumentTypeResponse>>> Create(
        [FromBody] CreateInstrumentTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<InstrumentTypeResponse>.Ok(result));
    }

    /// <summary>
    /// 修改仪器类型
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<InstrumentTypeResponse>>> Update(
        Guid id,
        [FromBody] UpdateInstrumentTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InstrumentTypeResponse>.Ok(result));
    }

    /// <summary>
    /// 禁用仪器类型
    /// </summary>
    [HttpPost("{id}/disable")]
    public async Task<ActionResult<ApiResponse>> Disable(Guid id, CancellationToken cancellationToken)
    {
        await _service.DisableAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// 获取所有仪器类型
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InstrumentTypeResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InstrumentTypeResponse>>.Ok(result));
    }

    /// <summary>
    /// 获取仪器类型详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InstrumentTypeResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<InstrumentTypeResponse>.Ok(result));
    }
}
