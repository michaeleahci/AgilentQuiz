using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgilentQuiz.Infrastructure.Data.Repositories;

/// <summary>
/// 仪器类型仓储实现
/// </summary>
public class InstrumentTypeRepository : IInstrumentTypeRepository
{
    private readonly AppDbContext _context;

    public InstrumentTypeRepository(AppDbContext context) => _context = context;

    public async Task<InstrumentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.InstrumentTypes.FindAsync(new object[] { id }, cancellationToken);

    public async Task<InstrumentType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => await _context.InstrumentTypes.FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

    public async Task<IReadOnlyList<InstrumentType>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.InstrumentTypes.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(InstrumentType instrumentType, CancellationToken cancellationToken = default)
        => await _context.InstrumentTypes.AddAsync(instrumentType, cancellationToken);

    public void Update(InstrumentType instrumentType) => _context.InstrumentTypes.Update(instrumentType);
}

/// <summary>
/// 仪器仓储实现
/// </summary>
public class InstrumentRepository : IInstrumentRepository
{
    private readonly AppDbContext _context;

    public InstrumentRepository(AppDbContext context) => _context = context;

    public async Task<Instrument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Instruments.FindAsync(new object[] { id }, cancellationToken);

    public async Task<Instrument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => await _context.Instruments.FirstOrDefaultAsync(i => i.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Instrument>> GetByTypeAsync(Guid instrumentTypeId, CancellationToken cancellationToken = default)
        => await _context.Instruments.AsNoTracking()
            .Where(i => i.InstrumentTypeId == instrumentTypeId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Instrument>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _context.Instruments.AsNoTracking()
            .Where(i => idList.Contains(i.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Instrument instrument, CancellationToken cancellationToken = default)
        => await _context.Instruments.AddAsync(instrument, cancellationToken);

    public void Update(Instrument instrument) => _context.Instruments.Update(instrument);

    /// <summary>
    /// 获取指定时间段内被占用的仪器ID。
    /// 使用 UPDLOCK + HOLDLOCK 在事务内锁定相关行与索引范围，
    /// 配合 Serializable 隔离级别防止并发下的超卖问题。
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetOccupiedInstrumentIdsAsync(
        Guid instrumentTypeId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        // 活跃预约状态：Pending(1), InUse(2)
        // 活跃预约项状态：Pending(1), Completed(2)
        const string sql = @"
SELECT DISTINCT ri.InstrumentId
FROM ReservationItems ri WITH (UPDLOCK, HOLDLOCK)
INNER JOIN Reservations r WITH (UPDLOCK, HOLDLOCK) ON ri.ReservationId = r.Id
WHERE r.InstrumentTypeId = {0}
  AND r.Status IN (1, 2)
  AND ri.Status IN (1, 2)
  AND r.StartTime < {1}
  AND r.EndTime > {2}";

        return await _context.Database.SqlQueryRaw<Guid>(sql,
            instrumentTypeId,
            endTime,
            startTime).ToListAsync(cancellationToken);
    }
}
