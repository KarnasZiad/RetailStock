using Microsoft.AspNetCore.SignalR;

namespace RetailStock.Api.Realtime;

/// <summary>
/// Real-time channel of the dashboards. A client subscribes either to every store
/// (head office) or to a single store (store manager).
/// </summary>
public sealed class StockHub : Hub<IStockClient>
{
    public const string AllStoresGroup = "stores:all";

    public static string StoreGroup(Guid storeId) => $"store:{storeId}";

    public Task SubscribeAll() => Groups.AddToGroupAsync(Context.ConnectionId, AllStoresGroup);

    public Task SubscribeStore(Guid storeId) => Groups.AddToGroupAsync(Context.ConnectionId, StoreGroup(storeId));

    public Task UnsubscribeStore(Guid storeId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, StoreGroup(storeId));
}
