using System.Net;
using System.Text.Json;
using RetailStock.Api.Endpoints;
using RetailStock.Application.Queries;
using RetailStock.Application.Stock;
using RetailStock.Domain.Stock;

namespace RetailStock.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class StockApiTests(RetailStockApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_reports_the_database_as_healthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Receipt_is_persisted_and_written_to_the_ledger()
    {
        var network = await factory.CreateNetworkAsync();

        var response = await _client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(12, "BL-2026-001"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stock = await response.ReadAsync<StockItemDto>();
        Assert.Equal(12, stock.OnHand);

        var movements = await (await _client.GetAsync($"/api/movements?productId={network.Product.Id}")).ReadAsync<List<MovementDto>>();
        var movement = Assert.Single(movements);
        Assert.Equal(MovementType.Receipt, movement.Type);
        Assert.Equal("BL-2026-001", movement.Reference);
    }

    [Fact]
    public async Task Sale_beyond_available_stock_is_rejected_with_a_problem_details()
    {
        var network = await factory.CreateNetworkAsync();
        await _client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(2, null));

        var response = await _client.PostJsonAsync($"{network.LineA}/sales", new QuantityRequest(3, "TICKET-1"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.ReadAsync<JsonElement>();
        Assert.Equal("stock.insufficient", problem.GetProperty("code").GetString());

        var stock = await (await _client.GetAsync($"/api/stock?productId={network.Product.Id}")).ReadAsync<List<StockItemDto>>();
        Assert.Equal(2, Assert.Single(stock).OnHand);
    }

    [Fact]
    public async Task Unknown_store_returns_404()
    {
        var network = await factory.CreateNetworkAsync();

        var response = await _client.PostJsonAsync($"/api/stores/{Guid.NewGuid()}/products/{network.Product.Id}/receipts", new QuantityRequest(1, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_reservations_never_oversell_the_stock()
    {
        const int available = 5;
        const int customers = 25;
        var network = await factory.CreateNetworkAsync();
        await _client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(available, null));

        // 25 customers try to book the last 5 units at the same time.
        var responses = await Task.WhenAll(Enumerable.Range(0, customers).Select(i =>
            factory.CreateClient().PostJsonAsync($"{network.LineA}/reservations", new ReservationRequest(1, $"WEB-{i}", 30))));

        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Conflict }));
        Assert.Equal(available, succeeded);

        var stock = Assert.Single(await (await _client.GetAsync($"/api/stock?productId={network.Product.Id}")).ReadAsync<List<StockItemDto>>());
        Assert.Equal(available, stock.Reserved);
        Assert.Equal(0, stock.Available);
        Assert.Equal(StockLevel.OutOfStock, stock.Level);
    }

    [Fact]
    public async Task Click_and_collect_reservation_lifecycle()
    {
        var network = await factory.CreateNetworkAsync();
        await _client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(10, null));

        var created = await _client.PostJsonAsync($"{network.LineA}/reservations", new ReservationRequest(3, "CMD-1001", null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var reservation = await created.ReadAsync<ReservationDto>();
        Assert.Equal(7, reservation.Stock.Available);

        var confirmed = await (await _client.PostAsync($"/api/reservations/{reservation.Id}/confirm", null)).ReadAsync<StockItemDto>();
        Assert.Equal(7, confirmed.OnHand);
        Assert.Equal(0, confirmed.Reserved);

        var twice = await _client.PostAsync($"/api/reservations/{reservation.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, twice.StatusCode);
    }

    [Fact]
    public async Task Released_reservation_makes_stock_available_again()
    {
        var network = await factory.CreateNetworkAsync();
        await _client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(4, null));
        var reservation = await (await _client.PostJsonAsync($"{network.LineA}/reservations", new ReservationRequest(4, "CMD-1002", null))).ReadAsync<ReservationDto>();

        var released = await (await _client.PostAsync($"/api/reservations/{reservation.Id}/release", null)).ReadAsync<StockItemDto>();

        Assert.Equal(4, released.Available);
    }

    [Fact]
    public async Task Alerts_list_low_and_out_of_stock_lines()
    {
        var network = await factory.CreateNetworkAsync();
        await _client.PostJsonAsync($"{network.LineA}/receipts", new QuantityRequest(2, null));
        await _client.PostJsonAsync($"{network.LineB}/receipts", new QuantityRequest(50, null));

        var alerts = await (await _client.GetAsync("/api/stock/alerts")).ReadAsync<List<StockItemDto>>();

        Assert.Contains(alerts, a => a.StoreId == network.StoreA.Id && a.ProductId == network.Product.Id && a.Level == StockLevel.Low);
        Assert.DoesNotContain(alerts, a => a.StoreId == network.StoreB.Id && a.ProductId == network.Product.Id);
    }
}
