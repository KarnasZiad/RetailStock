using RetailStock.Application.Abstractions;
using RetailStock.Domain.Common;
using RetailStock.Domain.Stock;
using RetailStock.Domain.Transfers;

namespace RetailStock.UnitTests.Application;

/// <summary>Records the order in which side effects happen.</summary>
internal sealed class Journal
{
    public List<string> Entries { get; } = [];
}

internal sealed class InMemoryStockRepository : IStockRepository
{
    public List<StockItem> Items { get; } = [];

    public List<StockMovement> Movements { get; } = [];

    public Task<StockItem?> GetAsync(Guid storeId, Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(i => i.StoreId == storeId && i.ProductId == productId));

    public Task<StockItem?> GetByIdAsync(Guid stockItemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(i => i.Id == stockItemId));

    public Task<StockItem?> GetByReservationAsync(Guid reservationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(i => i.Reservations.Any(r => r.Id == reservationId)));

    public Task<IReadOnlyList<Guid>> GetIdsWithExpiredReservationsAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(Items.Where(i => i.Reservations.Any(r => r.IsExpired(now))).Select(i => i.Id).ToList());

    public void Add(StockItem stockItem) => Items.Add(stockItem);

    public void AddMovements(IEnumerable<StockMovement> movements) => Movements.AddRange(movements);
}

internal sealed class InMemoryTransferRepository : ITransferRepository
{
    public List<Transfer> Transfers { get; } = [];

    public Task<Transfer?> GetAsync(Guid transferId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Transfers.SingleOrDefault(t => t.Id == transferId));

    public void Add(Transfer transfer) => Transfers.Add(transfer);
}

internal sealed class InMemoryReferenceData : IReferenceDataRepository
{
    public HashSet<Guid> Stores { get; } = [];

    public HashSet<Guid> Products { get; } = [];

    public Task<bool> StoreExistsAsync(Guid storeId, CancellationToken cancellationToken = default) => Task.FromResult(Stores.Contains(storeId));

    public Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default) => Task.FromResult(Products.Contains(productId));
}

internal sealed class RecordingUnitOfWork(Journal journal) : IUnitOfWork
{
    public int Commits { get; private set; }

    public int Discards { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Commits++;
        journal.Entries.Add("commit");
        return Task.CompletedTask;
    }

    public void DiscardChanges()
    {
        Discards++;
        journal.Entries.Add("discard");
    }
}

internal sealed class RecordingPublisher(Journal journal) : IStockEventPublisher
{
    public List<IDomainEvent> Published { get; } = [];

    public Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        Published.AddRange(events);
        journal.Entries.Add($"publish:{events.Count}");
        return Task.CompletedTask;
    }
}
