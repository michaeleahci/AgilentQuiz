using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgilentQuiz.Infrastructure.Data.Configurations;

/// <summary>
/// 仪器类型实体配置
/// </summary>
public class InstrumentTypeConfiguration : IEntityTypeConfiguration<InstrumentType>
{
    public void Configure(EntityTypeBuilder<InstrumentType> builder)
    {
        builder.ToTable("InstrumentTypes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        // 编码唯一索引
        builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("IX_InstrumentTypes_Code");
    }
}

/// <summary>
/// 仪器实体配置
/// </summary>
public class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("IX_Instruments_Code");
        builder.HasIndex(e => new { e.InstrumentTypeId, e.Status }).HasDatabaseName("IX_Instruments_TypeStatus");

        builder.HasOne(e => e.InstrumentType)
            .WithMany(t => t.Instruments)
            .HasForeignKey(e => e.InstrumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// 用户实体配置
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Phone).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(50);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(e => e.Phone).IsUnique().HasDatabaseName("IX_Users_Phone");
    }
}

/// <summary>
/// 预约单实体配置
/// </summary>
public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Phone).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.Property(e => e.IdempotencyKey).HasMaxLength(100);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        // 幂等键唯一索引（过滤 NULL，因为 SQL Server 中唯一索引允许多个 NULL）
        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("IX_Reservations_IdempotencyKey");

        // 按手机号查询索引
        builder.HasIndex(e => new { e.Phone, e.Status }).HasDatabaseName("IX_Reservations_PhoneStatus");

        // 冲突检测核心索引：仪器类型 + 状态 + 时间段
        builder.HasIndex(e => new { e.InstrumentTypeId, e.Status, e.StartTime, e.EndTime })
            .HasDatabaseName("IX_Reservations_TypeStatusTime");

        builder.HasOne(e => e.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.InstrumentType)
            .WithMany()
            .HasForeignKey(e => e.InstrumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// 预约明细实体配置
/// </summary>
public class ReservationItemConfiguration : IEntityTypeConfiguration<ReservationItem>
{
    public void Configure(EntityTypeBuilder<ReservationItem> builder)
    {
        builder.ToTable("ReservationItems");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(e => new { e.InstrumentId, e.Status }).HasDatabaseName("IX_ReservationItems_InstrumentStatus");
        builder.HasIndex(e => e.ReservationId).HasDatabaseName("IX_ReservationItems_ReservationId");

        builder.HasOne(e => e.Reservation)
            .WithMany(r => r.Items)
            .HasForeignKey(e => e.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Instrument)
            .WithMany(i => i.ReservationItems)
            .HasForeignKey(e => e.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// 通知记录实体配置
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).HasConversion<int>();
        builder.Property(e => e.Content).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(e => new { e.Status, e.RetryCount }).HasDatabaseName("IX_Notifications_StatusRetry");

        builder.HasOne(e => e.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Reservation)
            .WithMany()
            .HasForeignKey(e => e.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
