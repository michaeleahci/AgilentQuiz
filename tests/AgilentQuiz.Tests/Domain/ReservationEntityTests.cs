using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;
using Xunit;

namespace AgilentQuiz.Tests.Domain;

/// <summary>
/// 预约单领域实体测试
/// </summary>
public class ReservationEntityTests
{
    [Fact]
    public void Cancel_ShouldMarkAllItemsAndReservationAsCancelled()
    {
        // Arrange
        var reservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));
        reservation.AddItem(Guid.NewGuid());
        reservation.AddItem(Guid.NewGuid());

        // Act
        reservation.Cancel();

        // Assert
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.All(reservation.Items, item => Assert.Equal(ReservationItemStatus.Cancelled, item.Status));
    }

    [Fact]
    public void CancelItems_ShouldOnlyCancelSpecifiedInstruments()
    {
        // Arrange
        var reservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));
        var instrument1 = Guid.NewGuid();
        var instrument2 = Guid.NewGuid();
        reservation.AddItem(instrument1);
        reservation.AddItem(instrument2);

        // Act
        reservation.CancelItems(new[] { instrument1 });

        // Assert
        Assert.Equal(ReservationStatus.Pending, reservation.Status); // 仍有有效项
        var item1 = reservation.Items.First(i => i.InstrumentId == instrument1);
        var item2 = reservation.Items.First(i => i.InstrumentId == instrument2);
        Assert.Equal(ReservationItemStatus.Cancelled, item1.Status);
        Assert.Equal(ReservationItemStatus.Pending, item2.Status);
    }

    [Fact]
    public void CancelItems_WhenAllItemsCancelled_ShouldMarkReservationAsCancelled()
    {
        // Arrange
        var reservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));
        var instrument1 = Guid.NewGuid();
        reservation.AddItem(instrument1);

        // Act
        reservation.CancelItems(new[] { instrument1 });

        // Assert
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public void MarkDefaulted_ShouldMarkPendingItemsAsDefaulted()
    {
        // Arrange
        var reservation = new Reservation(
            Guid.NewGuid(), "13800138000", Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-4), DateTime.UtcNow.AddHours(-2));
        var instrument1 = Guid.NewGuid();
        var instrument2 = Guid.NewGuid();
        reservation.AddItem(instrument1);
        reservation.AddItem(instrument2);
        // 取消其中一个
        reservation.CancelItems(new[] { instrument1 });

        // Act
        reservation.MarkDefaulted();

        // Assert
        Assert.Equal(ReservationStatus.Defaulted, reservation.Status);
        var item1 = reservation.Items.First(i => i.InstrumentId == instrument1);
        var item2 = reservation.Items.First(i => i.InstrumentId == instrument2);
        Assert.Equal(ReservationItemStatus.Cancelled, item1.Status); // 已取消的不变
        Assert.Equal(ReservationItemStatus.Defaulted, item2.Status); // 待使用的变违约
    }
}
