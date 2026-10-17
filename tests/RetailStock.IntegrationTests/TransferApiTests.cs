using System.Net;
using RetailStock.Api.Endpoints;
using RetailStock.Application.Stock;
using RetailStock.Application.Transfers;
using RetailStock.Domain.Transfers;

namespace RetailStock.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class TransferApiTests(RetailStockApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<int> OnHandAsync(TestNetwork network, Guid storeId)
    {
        var lines = await (await _client.GetAsync($"/api/stock?storeId={storeId}&productId={network.Product.Id}")).ReadAsync<List<StockItemDto>>();
        return lines.SingleOrDefault()?.OnHand ?? 0;
    }

    [Fact]
    public async Task Transfer_moves_stock_between_stores()
    {
        var network = await factory.CreateNetworkAsync();
        await _client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(10, null));

        var created = await _client.PostJsonAsync("/api/transfers", new TransferRequest(network.StoreA.Id, network.StoreB.Id, network.Product.Id, 4, "Reequilibrage"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var transfer = await created.ReadAsync<TransferDto>();
        Assert.Equal(TransferStatus.InTransit, transfer.Status);
        Assert.Equal(6, await OnHandAsync(network, network.StoreA.Id));
        Assert.Equal(0, await OnHandAsync(network, network.StoreB.Id));

        var received = await (await _client.PostAsync($"/api/transfers/{transfer.Id}/receive", null)).ReadAsync<TransferDto>();

        Assert.Equal(TransferStatus.Received, received.Status);
        Assert.Equal(6, await OnHandAsync(network, network.StoreA.Id));
        Assert.Equal(4, await OnHandAsync(network, network.StoreB.Id));
    }

    [Fact]
    public async Task Cancelled_transfer_returns_stock_to_the_source()
    {
        var network = await factory.CreateNetworkAsync();
        await _client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(10, null));
        var transfer = await (await _client.PostJsonAsync("/api/transfers", new TransferRequest(network.StoreA.Id, network.StoreB.Id, network.Product.Id, 4, null))).ReadAsync<TransferDto>();

        var cancelled = await (await _client.PostAsync($"/api/transfers/{transfer.Id}/cancel", null)).ReadAsync<TransferDto>();
        var receiveAfterCancel = await _client.PostAsync($"/api/transfers/{transfer.Id}/receive", null);

        Assert.Equal(TransferStatus.Cancelled, cancelled.Status);
        Assert.Equal(10, await OnHandAsync(network, network.StoreA.Id));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, receiveAfterCancel.StatusCode);
    }

    [Fact]
    public async Task Transfer_to_the_same_store_is_rejected()
    {
        var network = await factory.CreateNetworkAsync();

        var response = await _client.PostJsonAsync("/api/transfers", new TransferRequest(network.StoreA.Id, network.StoreA.Id, network.Product.Id, 1, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
