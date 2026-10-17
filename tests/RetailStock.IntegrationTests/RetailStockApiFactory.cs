using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailStock.Domain.Catalog;
using RetailStock.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RetailStock.IntegrationTests;

/// <summary>
/// Boots the real API against a throw-away PostgreSQL container (Testcontainers):
/// real migrations, real SQL, real optimistic concurrency.
/// </summary>
public sealed class RetailStockApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("retailstock")
        .WithUsername("retailstock")
        .WithPassword("retailstock")
        .Build();

    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Added last, so these values override appsettings.json.
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:RetailStock"] = _postgres.GetConnectionString(),
            ["Database:SeedDemoData"] = "false",
            ["Simulator:Enabled"] = "false",
        }));
    }

    /// <summary>Creates an isolated store network for one test, so tests never share stock lines.</summary>
    public async Task<TestNetwork> CreateNetworkAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var network = new TestNetwork(
            new Store(Guid.NewGuid(), $"A-{suffix}", "Store A", "Paris"),
            new Store(Guid.NewGuid(), $"B-{suffix}", "Store B", "Lyon"),
            new Product(Guid.NewGuid(), $"SKU-{suffix}", "Test product", "Test", 10m));

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RetailStockDbContext>();
        db.Stores.AddRange(network.StoreA, network.StoreB);
        db.Products.Add(network.Product);
        await db.SaveChangesAsync();
        return network;
    }
}

public sealed record TestNetwork(Store StoreA, Store StoreB, Product Product)
{
    public string LineA => $"/api/stores/{StoreA.Id}/products/{Product.Id}";

    public string LineB => $"/api/stores/{StoreB.Id}/products/{Product.Id}";
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<RetailStockApiFactory>
{
    public const string Name = "api";
}

internal static class HttpClientExtensions
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(RetailStockApiFactory.Json);
        Assert.NotNull(body);
        return body;
    }

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PostAsJsonAsync(url, body, RetailStockApiFactory.Json);
}
