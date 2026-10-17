using Microsoft.Extensions.Options;
using RetailStock.Application.Stock;

namespace RetailStock.Api.Workers;

public sealed class ReservationExpiryOptions
{
    public const string SectionName = "ReservationExpiry";

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>Periodically gives back to the available stock the reservations that were not picked up in time.</summary>
internal sealed partial class ReservationExpiryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReservationExpiryOptions> options,
    ILogger<ReservationExpiryWorker> logger) : BackgroundService
{
    // The .NET 8 [LoggerMessage] generator needs an ILogger field (primary constructor parameters are not supported).
    private readonly ILogger<ReservationExpiryWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var expired = await scope.ServiceProvider.GetRequiredService<StockService>().ExpireReservationsAsync(stoppingToken);
                if (expired > 0)
                {
                    LogExpired(expired);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogFailure(ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} reservation(s) expired and released")]
    private partial void LogExpired(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Reservation expiry run failed")]
    private partial void LogFailure(Exception exception);
}
