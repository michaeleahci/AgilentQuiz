using AgilentQuiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgilentQuiz.Infrastructure.Data;

/// <summary>
/// 应用程序 DbContext
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<InstrumentType> InstrumentTypes => Set<InstrumentType>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 自动更新 UpdatedAt
        var entries = ChangeTracker.Entries<Entity>()
            .Where(e => e.State == EntityState.Modified);
        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
