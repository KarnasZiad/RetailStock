using Microsoft.Extensions.Options;
using RetailStock.Application.Queries;
using RetailStock.Application.Stock;
using RetailStock.Domain.Common;

namespace RetailStock.Api.Workers;

public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public bool Enabled { get; set; }

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>
/// Demo only: simulates the activity of the store network (till sales, web reservations,
/// replenishments) so that the real-time dashboard comes alive. Disabled by default.
/// </summary>
internal sealed partial class StoreActivitySimulator(
    IServiceScopeFactory scopeFactory,
    IOptions<SimulatorOptions> options,
    ILogger<StoreActivitySimulator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        LogStarted(options.Value.Interval);
        using var timer = new PeriodicTimer(options.Value.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SimulateOnceAsync(stoppingToken);
            }
            catch (DomainException)
            {
                // A refused operation is part of the game (e.g. a sale on an empty shelf).
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogFailure(ex);
            }
        }
    }

    private static async Task SimulateOnceAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var queries = services.GetRequiredService<IStockQueries>();
        var stock = services.GetRequiredService<StockService>();

        var items = await queries.GetStockAsync(null, null, cancellationToken);
        if (items.Count == 0)
        {
            return;
        }

        var item = items[Random.Shared.Next(items.Count)];
        var roll = Random.Shared.Next(100);

        if (item.Available <= item.AlertThreshold && roll < 35)
        {
            await stock.ReceiveAsync(new ReceiveStock(item.StoreId, item.ProductId, Random.Shared.Next(10, 25), "Reassort entrepot"), cancellationToken);
        }
        else if (roll < 70)
        {
            await stock.SellAsync(new RecordSale(item.StoreId, item.ProductId, Random.Shared.Next(1, 3), $"TICKET-{Random.Shared.Next(10000, 99999)}"), cancellationToken);
        }
        else if (roll < 90)
        {
            await stock.ReserveAsync(
                new ReserveStock(item.StoreId, item.ProductId, 1, $"WEB-{Random.Shared.Next(10000, 99999)}", TimeSpan.FromMinutes(2)),
                cancellationToken);
        }
        else
        {
            await stock.ReceiveAsync(new ReceiveStock(item.StoreId, item.ProductId, Random.Shared.Next(3, 8), "Retour client"), cancellationToken);
        }
    }

    private async Task SimulateOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await SimulateOnceAsync(scope.ServiceProvider, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Store activity simulator started (one operation every {Interval})")]
    private partial void LogStarted(TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Error, Message = "Simulated operation failed")]
    private partial void LogFailure(Exception exception);
}
