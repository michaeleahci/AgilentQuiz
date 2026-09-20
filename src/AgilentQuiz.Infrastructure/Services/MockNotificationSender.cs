using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AgilentQuiz.Infrastructure.Services;

/// <summary>
/// 通知发送器 Mock 实现（作业要求外部依赖可 Mock）。
/// 实际生产中可替换为企业微信/短信/邮件网关实现。
/// </summary>
public class MockNotificationSender : INotificationSender
{
    private readonly ILogger<MockNotificationSender> _logger;

    public MockNotificationSender(ILogger<MockNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Mock通知] 发送给用户 {UserId}, 类型={Type}, 内容={Content}",
            notification.UserId, notification.Type, notification.Content);

        // 模拟发送成功（可改为随机失败以测试重试逻辑）
        return Task.FromResult(true);
    }
}
