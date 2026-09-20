using AgilentQuiz.Domain.Entities;
using Xunit;

namespace AgilentQuiz.Tests.Domain;

/// <summary>
/// 用户领域实体测试
/// </summary>
public class UserEntityTests
{
    [Fact]
    public void IsBanned_WhenBanExpiryInFuture_ShouldReturnTrue()
    {
        var user = new User("13800138000");
        user.ApplyBan(TimeSpan.FromHours(24));

        Assert.True(user.IsBanned());
    }

    [Fact]
    public void IsBanned_WhenBanExpiryInPast_ShouldReturnFalse()
    {
        var user = new User("13800138000");
        user.ApplyBan(TimeSpan.FromHours(-1)); // 已过期

        Assert.False(user.IsBanned());
    }

    [Fact]
    public void IsBanned_WhenNoBan_ShouldReturnFalse()
    {
        var user = new User("13800138000");

        Assert.False(user.IsBanned());
    }

    [Fact]
    public void ApplyBan_ShouldSetBanExpiryTime()
    {
        var user = new User("13800138000");
        var duration = TimeSpan.FromHours(24);

        user.ApplyBan(duration);

        Assert.NotNull(user.BanExpiryTime);
        Assert.True(user.BanExpiryTime > DateTime.UtcNow.AddHours(23));
        Assert.True(user.BanExpiryTime < DateTime.UtcNow.AddHours(25));
    }
}
