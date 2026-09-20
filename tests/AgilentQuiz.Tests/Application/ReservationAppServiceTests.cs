using AgilentQuiz.Application.Common;
using AgilentQuiz.Application.DTOs.Reservation;
using AgilentQuiz.Application.Services;
using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;
using AgilentQuiz.Domain.Interfaces;
using AgilentQuiz.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgilentQuiz.Tests.Application;

/// <summary>
/// 预约应用服务测试
/// </summary>
public class ReservationAppServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IInstrumentTypeRepository> _typeRepoMock;
    private readonly Mock<IInstrumentRepository> _instrumentRepoMock;
    private readonly Mock<IReservationRepository> _reservationRepoMock;
    private readonly Mock<IReservationDomainService> _domainServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ReservationAppService _service;

    public ReservationAppServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _typeRepoMock = new Mock<IInstrumentTypeRepository>();
        _instrumentRepoMock = new Mock<IInstrumentRepository>();
        _reservationRepoMock = new Mock<IReservationRepository>();
        _domainServiceMock = new Mock<IReservationDomainService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        // 默认：事务直接执行
        _unitOfWorkMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<ReservationResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<Task<ReservationResponse>> op, CancellationToken _) => await op());

        _service = new ReservationAppService(
            _userRepoMock.Object,
            _typeRepoMock.Object,
            _instrumentRepoMock.Object,
            _reservationRepoMock.Object,
            _domainServiceMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<ReservationAppService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithIdempotencyKey_WhenExists_ShouldReturnExisting()
    {
        // Arrange
        var existingReservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));
        _reservationRepoMock.Setup(r => r.GetByIdempotencyKeyAsync("key-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReservation);

        var request = new CreateReservationRequest
        {
            Phone = "13800138000",
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4),
            InstrumentTypeId = Guid.NewGuid(),
            InstrumentIds = new List<Guid> { Guid.NewGuid() },
            IdempotencyKey = "key-123"
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.Equal(existingReservation.Id, result.Id);
        _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenInstrumentTypeNotFound_ShouldThrow()
    {
        // Arrange
        _typeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InstrumentType?)null);

        var request = new CreateReservationRequest
        {
            Phone = "13800138000",
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4),
            InstrumentTypeId = Guid.NewGuid(),
            InstrumentIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(request));
        Assert.Equal(ErrorCodes.InstrumentTypeNotFound, ex.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_ShouldThrowConflict()
    {
        // Arrange
        var user = new User("13800138000");
        _userRepoMock.Setup(r => r.GetByPhoneAsync("13800138000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _typeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstrumentType("类型A", "A"));
        _domainServiceMock.Setup(d => d.ValidateReservationAsync(It.IsAny<Reservation>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("仪器已被预约");

        var request = new CreateReservationRequest
        {
            Phone = "13800138000",
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4),
            InstrumentTypeId = Guid.NewGuid(),
            InstrumentIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(request));
        Assert.Equal(ErrorCodes.InstrumentConflict, ex.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ShouldCreateReservation()
    {
        // Arrange
        var user = new User("13800138000");
        _userRepoMock.Setup(r => r.GetByPhoneAsync("13800138000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _typeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstrumentType("类型A", "A"));
        _domainServiceMock.Setup(d => d.ValidateReservationAsync(It.IsAny<Reservation>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Callback<Reservation, CancellationToken>((r, _) => { });

        var request = new CreateReservationRequest
        {
            Phone = "13800138000",
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(4),
            InstrumentTypeId = Guid.NewGuid(),
            InstrumentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("13800138000", result.Phone);
        Assert.Equal(2, result.Items.Count);
        _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CancelAsync_WhenReservationNotFound_ShouldThrow()
    {
        _reservationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.CancelAsync(Guid.NewGuid(), new CancelReservationRequest()));
        Assert.Equal(ErrorCodes.ReservationNotFound, ex.Code);
    }

    [Fact]
    public async Task CancelAsync_WhenNotPending_ShouldThrow()
    {
        var reservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));
        reservation.Cancel(); // 已取消
        _reservationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.CancelAsync(reservation.Id, new CancelReservationRequest()));
        Assert.Equal(ErrorCodes.ReservationCannotCancel, ex.Code);
    }
}
