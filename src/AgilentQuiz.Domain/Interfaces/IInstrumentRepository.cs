using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Domain.Interfaces;

/// <summary>
/// 仪器仓储接口
/// </summary>
public interface IInstrumentRepository
{
    Task<Instrument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Instrument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Instrument>> GetByTypeAsync(Guid instrumentTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Instrument>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Instrument instrument, CancellationToken cancellationToken = default);
    void Update(Instrument instrument);

    /// <summary>
    /// 获取某时间段内被占用的仪器ID集合（用于冲突检测）
    /// </summary>
    Task<IReadOnlyList<Guid>> GetOccupiedInstrumentIdsAsync(
        Guid instrumentTypeId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}
