using AgilentQuiz.Domain.Entities;

namespace AgilentQuiz.Domain.Interfaces;

/// <summary>
/// 仪器类型仓储接口
/// </summary>
public interface IInstrumentTypeRepository
{
    Task<InstrumentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstrumentType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstrumentType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(InstrumentType instrumentType, CancellationToken cancellationToken = default);
    void Update(InstrumentType instrumentType);
}
