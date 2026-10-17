using RetailStock.Domain.Common;
using RetailStock.Domain.Stock;

namespace RetailStock.UnitTests.Domain;

public sealed class StockItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 2, 10, 0, 0, TimeSpan.Zero);

    private static StockItem NewItem(int onHand = 0, int threshold = 5)
    {
        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid(), Now, threshold);
        if (onHand > 0)
        {
            item.Receive(onHand, "initial", Now);
        }

        item.DequeueDomainEvents();
        return item;
    }

    [Fact]
    public void Receive_increases_on_hand_and_available()
    {
        var item = NewItem();

        item.Receive(12, "BL-001", Now);

        Assert.Equal(12, item.OnHand);
        Assert.Equal(12, item.Available);
        var change = Assert.IsType<StockChanged>(item.DomainEvents.First());
        Assert.Equal(MovementType.Receipt, change.Type);
        Assert.Equal(12, change.OnHandDelta);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Quantities_must_be_strictly_positive(int quantity)
    {
        var item = NewItem(onHand: 10);

        var error = Assert.Throws<DomainException>(() => item.Receive(quantity, null, Now));
        Assert.Equal(StockErrors.InvalidQuantity, error.Code);
        Assert.Throws<DomainException>(() => item.Sell(quantity, null, Now));
    }

    [Fact]
    public void Sale_cannot_exceed_available_stock()
    {
        var item = NewItem(onHand: 3);

        var error = Assert.Throws<InsufficientStockException>(() => item.Sell(4, "TICKET-1", Now));

        Assert.Equal(StockErrors.Insufficient, error.Code);
        Assert.Equal(4, error.Requested);
        Assert.Equal(3, error.Available);
        Assert.Equal(3, item.OnHand);
        Assert.Empty(item.DomainEvents);
    }

    [Fact]
    public void Reserved_quantity_cannot_be_sold()
    {
        var item = NewItem(onHand: 5);
        item.Reserve(4, "CMD-42", Now.AddHours(1), Now);

        Assert.Equal(1, item.Available);
        Assert.Throws<InsufficientStockException>(() => item.Sell(2, null, Now));
        item.Sell(1, null, Now);
        Assert.Equal(0, item.Available);
        Assert.Equal(4, item.OnHand);
    }

    [Fact]
    public void Reservation_cannot_exceed_available_stock()
    {
        var item = NewItem(onHand: 2);

        Assert.Throws<InsufficientStockException>(() => item.Reserve(3, "CMD-1", Now.AddHours(1), Now));
        Assert.Empty(item.Reservations);
        Assert.Equal(0, item.Reserved);
    }

    [Fact]
    public void Reservation_requires_a_reference_and_a_future_expiry()
    {
        var item = NewItem(onHand: 10);

        Assert.Equal(StockErrors.ReferenceRequired, Assert.Throws<DomainException>(() => item.Reserve(1, " ", Now.AddHours(1), Now)).Code);
        Assert.Equal(StockErrors.InvalidExpiry, Assert.Throws<DomainException>(() => item.Reserve(1, "CMD", Now, Now)).Code);
    }

    [Fact]
    public void Confirming_a_reservation_removes_goods_from_the_store()
    {
        var item = NewItem(onHand: 10);
        var reservation = item.Reserve(3, "CMD-7", Now.AddHours(2), Now);

        item.ConfirmReservation(reservation.Id, Now.AddHours(1));

        Assert.Equal(7, item.OnHand);
        Assert.Equal(0, item.Reserved);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void Releasing_a_reservation_makes_the_quantity_available_again()
    {
        var item = NewItem(onHand: 10);
        var reservation = item.Reserve(3, "CMD-8", Now.AddHours(2), Now);

        item.ReleaseReservation(reservation.Id, Now);

        Assert.Equal(10, item.OnHand);
        Assert.Equal(10, item.Available);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public void A_closed_reservation_cannot_be_confirmed_or_released_twice()
    {
        var item = NewItem(onHand: 10);
        var reservation = item.Reserve(3, "CMD-9", Now.AddHours(2), Now);
        item.ConfirmReservation(reservation.Id, Now);

        Assert.Equal(StockErrors.ReservationNotActive, Assert.Throws<DomainException>(() => item.ConfirmReservation(reservation.Id, Now)).Code);
        Assert.Equal(StockErrors.ReservationNotActive, Assert.Throws<DomainException>(() => item.ReleaseReservation(reservation.Id, Now)).Code);
        Assert.Equal(7, item.OnHand);
    }

    [Fact]
    public void An_expired_reservation_cannot_be_confirmed()
    {
        var item = NewItem(onHand: 10);
        var reservation = item.Reserve(3, "CMD-10", Now.AddMinutes(30), Now);

        var error = Assert.Throws<DomainException>(() => item.ConfirmReservation(reservation.Id, Now.AddHours(1)));

        Assert.Equal(StockErrors.ReservationExpired, error.Code);
        Assert.Equal(10, item.OnHand);
    }

    [Fact]
    public void Expired_reservations_are_released()
    {
        var item = NewItem(onHand: 10);
        item.Reserve(2, "CMD-A", Now.AddMinutes(10), Now);
        item.Reserve(3, "CMD-B", Now.AddHours(10), Now);

        var expired = item.ExpireReservations(Now.AddHours(1));

        Assert.Equal(1, expired);
        Assert.Equal(3, item.Reserved);
        Assert.Equal(7, item.Available);
    }

    [Fact]
    public void Inventory_adjustment_sets_the_counted_quantity()
    {
        var item = NewItem(onHand: 10);

        item.Adjust(8, "Inventaire annuel", Now);

        Assert.Equal(8, item.OnHand);
        var change = Assert.IsType<StockChanged>(item.DomainEvents.First());
        Assert.Equal(MovementType.Adjustment, change.Type);
        Assert.Equal(-2, change.OnHandDelta);
    }

    [Fact]
    public void Inventory_adjustment_cannot_go_below_reserved_quantity_nor_zero()
    {
        var item = NewItem(onHand: 10);
        item.Reserve(4, "CMD-11", Now.AddHours(1), Now);

        Assert.Equal(StockErrors.BelowReserved, Assert.Throws<DomainException>(() => item.Adjust(3, null, Now)).Code);
        Assert.Equal(StockErrors.NegativeStock, Assert.Throws<DomainException>(() => item.Adjust(-1, null, Now)).Code);
        Assert.Equal(10, item.OnHand);
    }

    [Fact]
    public void Adjustment_to_the_same_quantity_records_nothing()
    {
        var item = NewItem(onHand: 10);

        item.Adjust(10, null, Now);

        Assert.Empty(item.DomainEvents);
    }

    [Fact]
    public void Low_stock_alert_is_raised_when_crossing_the_threshold()
    {
        var item = NewItem(onHand: 8, threshold: 5);

        item.Sell(2, null, Now);
        Assert.DoesNotContain(item.DomainEvents, e => e is StockAlertRaised);

        item.Sell(1, null, Now);
        var alert = Assert.Single(item.DomainEvents.OfType<StockAlertRaised>());
        Assert.Equal(StockLevel.Low, alert.Level);
        Assert.Equal(5, alert.Available);
    }

    [Fact]
    public void Out_of_stock_alert_is_raised_when_nothing_is_available()
    {
        var item = NewItem(onHand: 2, threshold: 5);

        item.Reserve(2, "CMD-12", Now.AddHours(1), Now);

        var alert = Assert.Single(item.DomainEvents.OfType<StockAlertRaised>());
        Assert.Equal(StockLevel.OutOfStock, alert.Level);
        Assert.Equal(StockLevel.OutOfStock, item.Level);
    }

    [Fact]
    public void Alert_is_cleared_when_stock_is_replenished()
    {
        var item = NewItem(onHand: 2, threshold: 5);

        item.Receive(20, "Reassort", Now);

        Assert.Single(item.DomainEvents.OfType<StockAlertCleared>());
        Assert.Equal(StockLevel.Ok, item.Level);
    }

    [Fact]
    public void Transfer_shipment_consumes_available_stock_only()
    {
        var item = NewItem(onHand: 5);
        item.Reserve(3, "CMD-13", Now.AddHours(1), Now);

        Assert.Throws<InsufficientStockException>(() => item.ShipTransfer(3, Guid.NewGuid(), Now));
        item.ShipTransfer(2, Guid.NewGuid(), Now);
        Assert.Equal(3, item.OnHand);
        Assert.Equal(0, item.Available);
    }

    [Theory]
    [InlineData(0, 5, StockLevel.OutOfStock)]
    [InlineData(-1, 5, StockLevel.OutOfStock)]
    [InlineData(5, 5, StockLevel.Low)]
    [InlineData(1, 5, StockLevel.Low)]
    [InlineData(6, 5, StockLevel.Ok)]
    [InlineData(1, 0, StockLevel.Ok)]
    public void Stock_level_is_evaluated_from_available_quantity(int available, int threshold, StockLevel expected) =>
        Assert.Equal(expected, StockLevels.Evaluate(available, threshold));
}
