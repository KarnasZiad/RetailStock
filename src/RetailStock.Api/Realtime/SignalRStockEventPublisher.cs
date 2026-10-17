using Microsoft.AspNetCore.SignalR;
using RetailStock.Application.Abstractions;
using RetailStock.Domain.Common;
using RetailStock.Domain.Stock;
using RetailStock.Domain.Transfers;

namespace RetailStock.Api.Realtime;

/// <summary>Pushes committed domain events to the SignalR groups concerned. Best effort: never fails the operation.</summary>
internal sealed partial class SignalRStockEventPublisher(
    IHubContext<StockHub, IStockClient> hub,
    ILogger<SignalRStockEventPublisher> logger) : IStockEventPublisher
{
    public async Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            try
            {
                await PublishAsync(domainEvent);
            }
            catch (Exception ex)
            {
                LogPublishFailed(ex, domainEvent.GetType().Name);
            }
        }
    }

    private Task PublishAsync(IDomainEvent domainEvent) => domainEvent switch
    {
        StockChanged e => ToStores(e.StoreId, c => c.StockChanged(new StockChangedMessage(
            e.StockItemId, e.StoreId, e.ProductId, e.Type, e.OnHandDelta, e.ReservedDelta,
            e.OnHand, e.Reserved, e.Available, e.Level, e.Reference, e.OccurredAt))),

        StockAlertRaised e => ToStores(e.StoreId, c => c.StockAlert(new StockAlertMessage(
            e.StockItemId, e.StoreId, e.ProductId, e.Level, e.Available, e.Threshold, Cleared: false, e.OccurredAt))),

        StockAlertCleared e => ToStores(e.StoreId, c => c.StockAlert(new StockAlertMessage(
            e.StockItemId, e.StoreId, e.ProductId, StockLevel.Ok, e.Available, e.Threshold, Cleared: true, e.OccurredAt))),

        TransferStatusChanged e => ToStores(
            [e.SourceStoreId, e.DestinationStoreId],
            c => c.TransferUpdated(new TransferUpdatedMessage(
                e.TransferId, e.SourceStoreId, e.DestinationStoreId, e.ProductId, e.Quantity, e.Status, e.OccurredAt))),

        _ => Task.CompletedTask,
    };

    private Task ToStores(Guid storeId, Func<IStockClient, Task> send) => ToStores([storeId], send);

    private async Task ToStores(Guid[] storeIds, Func<IStockClient, Task> send)
    {
        await send(hub.Clients.Group(StockHub.AllStoresGroup));
        await send(hub.Clients.Groups(storeIds.Select(StockHub.StoreGroup).ToList()));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to push {EventType} to SignalR clients")]
    private partial void LogPublishFailed(Exception exception, string eventType);
}
