using RetailStock.Domain.Common;
using RetailStock.Domain.Stock;
using RetailStock.Domain.Transfers;

namespace RetailStock.UnitTests.Domain;

public sealed class TransferTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_transfer_is_in_transit()
    {
        var transfer = Transfer.Request(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4, "  Reequilibrage  ", Now);

        Assert.Equal(TransferStatus.InTransit, transfer.Status);
        Assert.Equal("Reequilibrage", transfer.Note);
        var changed = Assert.IsType<TransferStatusChanged>(Assert.Single(transfer.DomainEvents));
        Assert.Equal(TransferStatus.InTransit, changed.Status);
    }

    [Fact]
    public void Source_and_destination_must_differ()
    {
        var store = Guid.NewGuid();

        var error = Assert.Throws<DomainException>(() => Transfer.Request(store, store, Guid.NewGuid(), 1, null, Now));

        Assert.Equal(StockErrors.SameStore, error.Code);
    }

    [Fact]
    public void Quantity_must_be_positive()
    {
        var error = Assert.Throws<DomainException>(() => Transfer.Request(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, null, Now));

        Assert.Equal(StockErrors.InvalidQuantity, error.Code);
    }

    [Fact]
    public void A_received_transfer_cannot_be_received_again_nor_cancelled()
    {
        var transfer = Transfer.Request(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4, null, Now);
        transfer.MarkReceived(Now.AddDays(1));

        Assert.Equal(TransferStatus.Received, transfer.Status);
        Assert.Equal(Now.AddDays(1), transfer.CompletedAt);
        Assert.Equal(StockErrors.TransferNotInTransit, Assert.Throws<DomainException>(() => transfer.MarkReceived(Now)).Code);
        Assert.Equal(StockErrors.TransferNotInTransit, Assert.Throws<DomainException>(() => transfer.Cancel(Now)).Code);
    }

    [Fact]
    public void A_cancelled_transfer_cannot_be_received()
    {
        var transfer = Transfer.Request(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4, null, Now);
        transfer.Cancel(Now);

        Assert.Throws<DomainException>(() => transfer.MarkReceived(Now));
        Assert.Equal(TransferStatus.Cancelled, transfer.Status);
    }
}
