using AgilentQuiz.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgilentQuiz.Infrastructure.BackgroundServices;

/// <summary>
/// 通知分发定时任务：定期扫描待发送通知并发送
/// </summary>
public class NotificationDispatchHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationDispatchHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public NotificationDispatchHostedService(
        IServiceProvider serviceProvider,
        ILogger<NotificationDispatchHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationDispatchHostedService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<INotificationDispatchService>();
                await service.DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching notifications");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

/// <summary>
/// 违约处理定时任务：定期扫描已结束未取消的预约，标记违约并禁用用户
/// </summary>
public class DefaultHandlingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DefaultHandlingHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public DefaultHandlingHostedService(
        IServiceProvider serviceProvider,
        ILogger<DefaultHandlingHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DefaultHandlingHostedService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDefaultHandlingService>();
                await service.ProcessDefaultsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing defaults");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
