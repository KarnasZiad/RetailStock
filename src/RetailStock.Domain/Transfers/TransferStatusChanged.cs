using RetailStock.Domain.Common;

namespace RetailStock.Domain.Transfers;

public sealed record TransferStatusChanged(
    Guid TransferId,
    Guid SourceStoreId,
    Guid DestinationStoreId,
    Guid ProductId,
    int Quantity,
    TransferStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;
