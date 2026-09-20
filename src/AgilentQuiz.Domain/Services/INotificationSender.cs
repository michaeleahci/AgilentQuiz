using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Domain.Services;

/// <summary>
/// 通知发送服务接口（外部依赖，可 Mock）
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// 发送通知
    /// </summary>
    /// <param name="notification">通知记录</param>
    /// <param name="cancellationToken"></param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
