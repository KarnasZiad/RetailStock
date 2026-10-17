using RetailStock.Domain.Common;

namespace RetailStock.Application.Abstractions;

/// <summary>Pushes committed domain events to the outside world (SignalR dashboards...).</summary>
public interface IStockEventPublisher
{
    Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default);
}
