using AgilentQuiz.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgilentQuiz.Application;

/// <summary>
/// Application 层依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IReservationAppService, ReservationAppService>();
        services.AddScoped<IInstrumentTypeAppService, InstrumentTypeAppService>();
        services.AddScoped<IInstrumentAppService, InstrumentAppService>();
        services.AddScoped<INotificationDispatchService, NotificationDispatchService>();
        services.AddScoped<IDefaultHandlingService, DefaultHandlingService>();
        return services;
    }
}
