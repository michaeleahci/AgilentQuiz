using AgilentQuiz.Application.Services;
using AgilentQuiz.Domain.Interfaces;
using AgilentQuiz.Domain.Services;
using AgilentQuiz.Infrastructure.BackgroundServices;
using AgilentQuiz.Infrastructure.Data;
using AgilentQuiz.Infrastructure.Data.Repositories;
using AgilentQuiz.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgilentQuiz.Infrastructure;

/// <summary>
/// Infrastructure 层依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext (SQL Server)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IInstrumentTypeRepository, InstrumentTypeRepository>();
        services.AddScoped<IInstrumentRepository, InstrumentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Domain services
        services.AddScoped<IReservationDomainService, ReservationDomainService>();

        // Notification sender (Mock)
        services.AddSingleton<INotificationSender, MockNotificationSender>();

        // Hosted services (定时任务)
        services.AddHostedService<NotificationDispatchHostedService>();
        services.AddHostedService<DefaultHandlingHostedService>();

        return services;
    }
}
