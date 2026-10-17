using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using RetailStock.Application.Common;
using RetailStock.Application.Stock;
using RetailStock.Application.Transfers;

namespace RetailStock.UnitTests.Application;

internal sealed class ServiceFixture
{
    public ServiceFixture()
    {
        UnitOfWork = new RecordingUnitOfWork(Journal);
        Publisher = new RecordingPublisher(Journal);
        ReferenceData.Stores.UnionWith([StoreA, StoreB]);
        ReferenceData.Products.Add(Product);

        var runner = new OperationRunner(UnitOfWork, Stock, Publisher, NullLogger<OperationRunner>.Instance);
        StockService = new StockService(Stock, ReferenceData, runner, Clock);
        TransferService = new TransferService(Stock, Transfers, ReferenceData, runner, Clock);
    }

    public Guid StoreA { get; } = Guid.NewGuid();

    public Guid StoreB { get; } = Guid.NewGuid();

    public Guid Product { get; } = Guid.NewGuid();

    public Journal Journal { get; } = new();

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero));

    public InMemoryStockRepository Stock { get; } = new();

    public InMemoryTransferRepository Transfers { get; } = new();

    public InMemoryReferenceData ReferenceData { get; } = new();

    public RecordingUnitOfWork UnitOfWork { get; }

    public RecordingPublisher Publisher { get; }

    public StockService StockService { get; }

    public TransferService TransferService { get; }
}
