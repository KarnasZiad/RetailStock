using RetailStock.Application.Common;
using RetailStock.Application.Stock;
using RetailStock.Application.Transfers;
using RetailStock.Domain.Stock;
using RetailStock.Domain.Transfers;

namespace RetailStock.UnitTests.Application;

public sealed class TransferServiceTests
{
    private readonly ServiceFixture _f = new();

    private async Task<TransferDto> ShipAsync(int stock, int quantity)
    {
        await _f.StockService.ReceiveAsync(new ReceiveStock(_f.StoreA, _f.Product, stock, null));
        return await _f.TransferService.RequestAsync(new RequestTransfer(_f.StoreA, _f.StoreB, _f.Product, quantity, null));
    }

    private StockItem? StockOf(Guid store) => _f.Stock.Items.SingleOrDefault(i => i.StoreId == store);

    [Fact]
    public async Task Requesting_a_transfer_ships_the_goods_from_the_source_store()
    {
        var transfer = await ShipAsync(stock: 10, quantity: 4);

        Assert.Equal(TransferStatus.InTransit, transfer.Status);
        Assert.Equal(6, StockOf(_f.StoreA)!.OnHand);
        Assert.Null(StockOf(_f.StoreB));
        Assert.Equal(MovementType.TransferOut, _f.Stock.Movements[^1].Type);
    }

    [Fact]
    public async Task Receiving_a_transfer_adds_the_goods_to_the_destination_store()
    {
        var transfer = await ShipAsync(stock: 10, quantity: 4);

        var received = await _f.TransferService.ReceiveAsync(transfer.Id);

        Assert.Equal(TransferStatus.Received, received.Status);
        Assert.Equal(6, StockOf(_f.StoreA)!.OnHand);
        Assert.Equal(4, StockOf(_f.StoreB)!.OnHand);
    }

    [Fact]
    public async Task Cancelling_a_transfer_returns_the_goods_to_the_source_store()
    {
        var transfer = await ShipAsync(stock: 10, quantity: 4);

        await _f.TransferService.CancelAsync(transfer.Id);

        Assert.Equal(10, StockOf(_f.StoreA)!.OnHand);
        Assert.Equal(MovementType.TransferReturn, _f.Stock.Movements[^1].Type);
    }

    [Fact]
    public async Task A_transfer_cannot_ship_more_than_the_available_stock()
    {
        await Assert.ThrowsAsync<InsufficientStockException>(() => ShipAsync(stock: 3, quantity: 4));

        Assert.Empty(_f.Transfers.Transfers);
        Assert.Equal(3, StockOf(_f.StoreA)!.OnHand);
    }

    [Fact]
    public async Task A_transfer_to_an_unknown_store_is_refused()
    {
        await _f.StockService.ReceiveAsync(new ReceiveStock(_f.StoreA, _f.Product, 10, null));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _f.TransferService.RequestAsync(new RequestTransfer(_f.StoreA, Guid.NewGuid(), _f.Product, 1, null)));
    }
}
