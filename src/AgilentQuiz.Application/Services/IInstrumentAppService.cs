using AgilentQuiz.Application.DTOs.Instrument;

namespace AgilentQuiz.Application.Services;

/// <summary>
/// 仪器管理应用服务接口
/// </summary>
public interface IInstrumentAppService
{
    Task<InstrumentResponse> CreateAsync(CreateInstrumentRequest request, CancellationToken cancellationToken = default);
    Task<InstrumentResponse> UpdateAsync(Guid id, UpdateInstrumentRequest request, CancellationToken cancellationToken = default);
    Task MarkFaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkScrappedAsync(Guid id, CancellationToken cancellationToken = default);
    Task RecoverAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstrumentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstrumentResponse>> GetByTypeAsync(Guid instrumentTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查看资源使用情况
    /// </summary>
    Task<IReadOnlyList<InstrumentUsageResponse>> GetUsageAsync(Guid? instrumentTypeId, CancellationToken cancellationToken = default);
}
