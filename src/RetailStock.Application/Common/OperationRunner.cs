using Microsoft.Extensions.Logging;
using RetailStock.Application.Abstractions;
using RetailStock.Domain.Common;
using RetailStock.Domain.Stock;

namespace RetailStock.Application.Common;

/// <summary>Aggregates touched by an operation, whose domain events must be dispatched.</summary>
public sealed class OperationContext
{
    private readonly List<AggregateRoot> _aggregates = [];

    internal IReadOnlyList<AggregateRoot> Aggregates => _aggregates;

    public T Track<T>(T aggregate)
        where T : AggregateRoot
    {
        if (!_aggregates.Contains(aggregate))
        {
            _aggregates.Add(aggregate);
        }

        return aggregate;
    }
}

/// <summary>
/// Runs a write operation as a unit of work:
/// <list type="number">
///   <item>executes the business logic on freshly loaded aggregates;</item>
///   <item>appends a ledger entry for every stock change, in the same transaction;</item>
///   <item>commits, retrying the whole operation on optimistic concurrency conflicts;</item>
///   <item>publishes the domain events only once the transaction is committed.</item>
/// </list>
/// </summary>
public sealed partial class OperationRunner(
    IUnitOfWork unitOfWork,
    IStockRepository stockRepository,
    IStockEventPublisher publisher,
    ILogger<OperationRunner> logger)
{
    // The .NET 8 [LoggerMessage] generator needs an ILogger field (primary constructor parameters are not supported).
    private readonly ILogger<OperationRunner> _logger = logger;

    public const int MaxAttempts = 5;

    public async Task<TResult> RunAsync<TResult>(Func<OperationContext, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            var context = new OperationContext();
            try
            {
                var result = await operation(context);

                var events = context.Aggregates.SelectMany(a => a.DequeueDomainEvents()).ToList();
                stockRepository.AddMovements(events.OfType<StockChanged>().Select(StockMovement.From));

                await unitOfWork.CommitAsync(cancellationToken);
                await publisher.PublishAsync(events, cancellationToken);
                return result;
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                unitOfWork.DiscardChanges();
                LogConcurrencyRetry(attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(5, 25) * attempt), cancellationToken);
            }
            catch
            {
                unitOfWork.DiscardChanges();
                throw;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Optimistic concurrency conflict, retrying (attempt {Attempt})")]
    private partial void LogConcurrencyRetry(int attempt);
}
