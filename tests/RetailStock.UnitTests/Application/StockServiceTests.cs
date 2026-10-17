using RetailStock.Application.Common;
using RetailStock.Application.Stock;
using RetailStock.Domain.Stock;

namespace RetailStock.UnitTests.Application;

public sealed class StockServiceTests
{
    private readonly ServiceFixture _f = new();

    [Fact]
    public async Task First_receipt_creates_the_stock_line()
    {
        var result = await _f.StockService.ReceiveAsync(new ReceiveStock(_f.StoreA, _f.Product, 10, "BL-1"));

        Assert.Equal(10, result.OnHand);
        Assert.Single(_f.Stock.Items);
        Assert.Equal(1, _f.UnitOfWork.Commits);
    }

    [Fact]
    public async Task Every_stock_change_is_written_to_the_ledger()
    {
        await _f.StockService.ReceiveAsync(new ReceiveStock(_f.StoreA, _f.Product, 10, "BL-1"));
        await _f.StockService.SellAsync(new RecordSale(_f.StoreA, _f.Product, 2, "T-1"));

        Assert.Collection(
            _f.Stock.Movements,
            m => Assert.Equal((MovementType.Receipt, 10, 10), (m.Type, m.OnHandDelta, m.OnHandAfter)),
            m => Assert.Equal((MovementType.Sale, -2, 8), (m.Type, m.OnHandDelta, m.OnHandAfter)));
    }

    [Fact]
    public async Task Events_are_published_only_after_the_commit()
    {
        await _f.StockService.ReceiveAsync(new ReceiveStock(_f.StoreA, _f.Product, 10, null));

        Assert.Equal(new[] { "commit", "publish:2" }, _f.Journal.Entries);
        Assert.Contains(_f.Publisher.Published, e => e is StockChanged);
        Assert.Contains(_f.Publisher.Published, e => e is StockAlertCleared);
    }

    [Fact]
    public async Task A_refused_operation_commits_and_publishes_nothing()
    {
        await _f.StockService.ReceiveAsync(new ReceiveStock(_f.StoreA, _f.Product, 1, null));
        _f.Journal.Entries.Clear();

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            _f.StockService.SellAsync(new RecordSale(_f.StoreA, _f.Product, 5, null)));

        Assert.Equal(new[] { "discard" }, _f.Journal.Entries);
    }

    [Fact]
    public async Task Unknown_store_is_reported_as_not_found()
    {
        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            _f.StockService.ReceiveAsync(new ReceiveStock(Guid.NewGuid(), _f.Product, 1, null)));

        Assert.Equal("Store", error.Resource);
    }

    [Fact]
    public async Task Selling_a_product_never_stocked_in_the_store_is_refused()
    {
        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            _f.StockService.SellAsync(new RecordSale(_f.StoreB, _f.Product, 1, null)));
    }

    [Fact]
    public async Task Reservation_uses_the_default_time_to_live()
    {
        await _f.StockService.ReceiveAsync(new ReceiveStock(_f.StoreA, _f.Product, 5, null));

        var reservation = await _f.StockService.ReserveAsync(new ReserveStock(_f.StoreA, _f.Product, 2, "CMD-1"));

        Assert.Equal(_f.Clock.GetUtcNow() + StockService.DefaultReservationTimeToLive, reservation.ExpiresAt);
        Assert.Equal(3, reservation.Stock.Available);
    }

    [Fact]
    public async Task Expiry_job_releases_reservations_past_their_deadline()
    {
        await _f.StockService.ReceiveAsync(new ReceiveStock(_f.StoreA, _f.Product, 5, null));
        await _f.StockService.ReserveAsync(new ReserveStock(_f.StoreA, _f.Product, 2, "CMD-1", TimeSpan.FromMinutes(5)));

        _f.Clock.Advance(TimeSpan.FromMinutes(6));
        var expired = await _f.StockService.ExpireReservationsAsync();

        Assert.Equal(1, expired);
        Assert.Equal(5, _f.Stock.Items.Single().Available);
        Assert.Equal(MovementType.ReservationExpired, _f.Stock.Movements[^1].Type);
    }

    [Fact]
    public async Task Unknown_reservation_is_reported_as_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _f.StockService.ConfirmReservationAsync(Guid.NewGuid()));
    }
}
