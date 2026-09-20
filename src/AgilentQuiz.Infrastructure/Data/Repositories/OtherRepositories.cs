using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;
using AgilentQuiz.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgilentQuiz.Infrastructure.Data.Repositories;

/// <summary>
/// 用户仓储实现
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Users.FindAsync(new object[] { id }, cancellationToken);

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await _context.Users.AddAsync(user, cancellationToken);

    public void Update(User user) => _context.Users.Update(user);
}

/// <summary>
/// 预约仓储实现
/// </summary>
public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context) => _context = context;

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Reservations
            .Include(r => r.Items)
                .ThenInclude(i => i.Instrument)
            .Include(r => r.InstrumentType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
        => await _context.Reservations.AsNoTracking()
            .Include(r => r.Items)
                .ThenInclude(i => i.Instrument)
            .Include(r => r.InstrumentType)
            .Where(r => r.Phone == phone)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetActiveByPhoneAsync(string phone, CancellationToken cancellationToken = default)
        => await _context.Reservations.AsNoTracking()
            .Include(r => r.Items)
                .ThenInclude(i => i.Instrument)
            .Include(r => r.InstrumentType)
            .Where(r => r.Phone == phone &&
                        (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.InUse))
            .OrderBy(r => r.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<Reservation?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        => await _context.Reservations.AsNoTracking()
            .Include(r => r.Items)
                .ThenInclude(i => i.Instrument)
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
        => await _context.Reservations.AddAsync(reservation, cancellationToken);

    public void Update(Reservation reservation) => _context.Reservations.Update(reservation);

    public async Task<IReadOnlyList<Reservation>> GetActiveByInstrumentAsync(Guid instrumentId, CancellationToken cancellationToken = default)
        => await _context.Reservations.AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.Items.Any(i => i.InstrumentId == instrumentId &&
                                          (i.Status == ReservationItemStatus.Pending || i.Status == ReservationItemStatus.Completed)) &&
                        (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.InUse))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetPendingForDefaultCheckAsync(DateTime now, CancellationToken cancellationToken = default)
        => await _context.Reservations
            .Include(r => r.Items)
            .Where(r => r.Status == ReservationStatus.Pending && r.EndTime < now)
            .ToListAsync(cancellationToken);
}

/// <summary>
/// 通知仓储实现
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await _context.Notifications.AddAsync(notification, cancellationToken);

    public void Update(Notification notification) => _context.Notifications.Update(notification);

    public async Task<IReadOnlyList<Notification>> GetPendingNotificationsAsync(int maxRetryCount, CancellationToken cancellationToken = default)
        => await _context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending && n.RetryCount < maxRetryCount)
            .OrderBy(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
}
