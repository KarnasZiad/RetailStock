namespace RetailStock.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
