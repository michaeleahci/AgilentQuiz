using AgilentQuiz.Application.DTOs.Instrument;

namespace AgilentQuiz.Application.Services;

/// <summary>
/// 仪器类型管理应用服务接口
/// </summary>
public interface IInstrumentTypeAppService
{
    Task<InstrumentTypeResponse> CreateAsync(CreateInstrumentTypeRequest request, CancellationToken cancellationToken = default);
    Task<InstrumentTypeResponse> UpdateAsync(Guid id, UpdateInstrumentTypeRequest request, CancellationToken cancellationToken = default);
    Task DisableAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstrumentTypeResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InstrumentTypeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
