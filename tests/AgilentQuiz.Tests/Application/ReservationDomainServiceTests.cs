using AgilentQuiz.Application.Services;
using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;
using AgilentQuiz.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgilentQuiz.Tests.Application;

/// <summary>
/// 预约领域服务测试
/// </summary>
public class ReservationDomainServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IInstrumentRepository> _instrumentRepoMock;
    private readonly ReservationDomainService _service;

    public ReservationDomainServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _instrumentRepoMock = new Mock<IInstrumentRepository>();
        _service = new ReservationDomainService(
            _userRepoMock.Object,
            _instrumentRepoMock.Object,
            NullLogger<ReservationDomainService>.Instance);
    }

    [Fact]
    public async Task ValidateReservation_WhenStartTimeWithinOneHour_ShouldReturnError()
    {
        // Arrange
        var reservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(30), // 不足1小时
            DateTime.UtcNow.AddHours(2));
        var instrumentIds = new List<Guid> { Guid.NewGuid() };

        // Act
        var result = await _service.ValidateReservationAsync(reservation, instrumentIds);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("1", result); // 包含1小时字样
    }

    [Fact]
    public async Task ValidateReservation_WhenUserBanned_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("13800138000");
        user.ApplyBan(TimeSpan.FromHours(24));
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var reservation = new Reservation(
            userId, "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));
        var instrumentIds = new List<Guid> { Guid.NewGuid() };

        // 仪器存在且可用
        var instrument = new Instrument(Guid.NewGuid(), "Test", "T-001");
        _instrumentRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instrument> { instrument });
        _instrumentRepoMock.Setup(r => r.GetOccupiedInstrumentIdsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        var result = await _service.ValidateReservationAsync(reservation, instrumentIds);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("禁用", result);
    }

    [Fact]
    public async Task ValidateReservation_WhenInstrumentNotAvailable_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User("13800138000"));

        var instrumentId = Guid.NewGuid();
        var instrument = new Instrument(Guid.NewGuid(), "Test", "T-001");
        instrument.MarkFault(); // 故障
        _instrumentRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instrument> { instrument });

        var reservation = new Reservation(
            userId, "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));

        // Act
        var result = await _service.ValidateReservationAsync(reservation, new List<Guid> { instrumentId });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("不可用", result);
    }

    [Fact]
    public async Task ValidateReservation_WhenTimeConflict_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User("13800138000"));

        var instrumentId = Guid.NewGuid();
        var instrument = new Instrument(Guid.NewGuid(), "Test", "T-001");
        _instrumentRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instrument> { instrument });
        // 模拟该仪器已被占用
        _instrumentRepoMock.Setup(r => r.GetOccupiedInstrumentIdsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { instrumentId });

        var reservation = new Reservation(
            userId, "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));

        // Act
        var result = await _service.ValidateReservationAsync(reservation, new List<Guid> { instrumentId });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("已被预约", result);
    }

    [Fact]
    public async Task ValidateReservation_WhenAllValid_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User("13800138000"));

        var instrumentId = Guid.NewGuid();
        var instrument = new Instrument(Guid.NewGuid(), "Test", "T-001");
        _instrumentRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instrument> { instrument });
        _instrumentRepoMock.Setup(r => r.GetOccupiedInstrumentIdsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var reservation = new Reservation(
            userId, "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));

        // Act
        var result = await _service.ValidateReservationAsync(reservation, new List<Guid> { instrumentId });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsDefaulted_WhenEndTimePassedAndPending_ShouldReturnTrue()
    {
        var reservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-4), DateTime.UtcNow.AddHours(-2));
        // 状态默认为 Pending

        var result = _service.IsDefaulted(reservation, DateTime.UtcNow);

        Assert.True(result);
    }

    [Fact]
    public void IsDefaulted_WhenEndTimeNotPassed_ShouldReturnFalse()
    {
        var reservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));

        var result = _service.IsDefaulted(reservation, DateTime.UtcNow);

        Assert.False(result);
    }

    [Fact]
    public void IsDefaulted_WhenAlreadyCancelled_ShouldReturnFalse()
    {
        var reservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-4), DateTime.UtcNow.AddHours(-2));
        reservation.Cancel();

        var result = _service.IsDefaulted(reservation, DateTime.UtcNow);

        Assert.False(result);
    }
}
