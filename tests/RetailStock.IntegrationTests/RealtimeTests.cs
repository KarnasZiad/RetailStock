using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RetailStock.Api.Endpoints;
using RetailStock.Api.Realtime;
using RetailStock.Domain.Stock;

namespace RetailStock.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class RealtimeTests(RetailStockApiFactory factory)
{
    private HubConnection Connect() =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/stock"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

    [Fact]
    public async Task Dashboard_receives_stock_changes_and_alerts_in_real_time()
    {
        var network = await factory.CreateNetworkAsync();
        var client = factory.CreateClient();
        await client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(6, null));

        var change = new TaskCompletionSource<StockChangedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var alert = new TaskCompletionSource<StockAlertMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = Connect();
        connection.On<StockChangedMessage>(nameof(IStockClient.StockChanged), m =>
        {
            if (m.ProductId == network.Product.Id)
            {
                change.TrySetResult(m);
            }
        });
        connection.On<StockAlertMessage>(nameof(IStockClient.StockAlert), m =>
        {
            if (m.ProductId == network.Product.Id)
            {
                alert.TrySetResult(m);
            }
        });
        await connection.StartAsync();
        await connection.InvokeAsync(nameof(StockHub.SubscribeAll));

        // 6 - 2 = 4 units left: below the default threshold (5) => low stock alert.
        await client.PostJsonAsync($"{network.LineA}/sales", new QuantityRequest(2, "TICKET-42"));

        var changed = await change.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(MovementType.Sale, changed.Movement);
        Assert.Equal(4, changed.Available);
        Assert.Equal("TICKET-42", changed.Reference);

        var raised = await alert.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(StockLevel.Low, raised.Level);
        Assert.False(raised.Cleared);
    }

    [Fact]
    public async Task Store_subscription_only_receives_its_own_store()
    {
        var network = await factory.CreateNetworkAsync();
        var client = factory.CreateClient();
        var received = new List<StockChangedMessage>();

        await using var connection = Connect();
        connection.On<StockChangedMessage>(nameof(IStockClient.StockChanged), m =>
        {
            lock (received)
            {
                received.Add(m);
            }
        });
        await connection.StartAsync();
        await connection.InvokeAsync(nameof(StockHub.SubscribeStore), network.StoreB.Id);

        await client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(3, null));
        await client.PostJsonAsync($"{network.LineB}/receipts", new QuantityRequest(7, null));

        await WaitUntilAsync(() => received.Any(m => m.StoreId == network.StoreB.Id));
        lock (received)
        {
            Assert.All(received, m => Assert.Equal(network.StoreB.Id, m.StoreId));
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for a SignalR message.");
            await Task.Delay(50);
        }
    }
}
