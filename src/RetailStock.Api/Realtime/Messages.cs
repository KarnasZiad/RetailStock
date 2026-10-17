using RetailStock.Domain.Stock;
using RetailStock.Domain.Transfers;

namespace RetailStock.Api.Realtime;

public sealed record StockChangedMessage(
    Guid StockItemId,
    Guid StoreId,
    Guid ProductId,
    MovementType Movement,
    int OnHandDelta,
    int ReservedDelta,
    int OnHand,
    int Reserved,
    int Available,
    StockLevel Level,
    string? Reference,
    DateTimeOffset OccurredAt);

public sealed record StockAlertMessage(
    Guid StockItemId,
    Guid StoreId,
    Guid ProductId,
    StockLevel Level,
    int Available,
    int Threshold,
    bool Cleared,
    DateTimeOffset OccurredAt);

public sealed record TransferUpdatedMessage(
    Guid TransferId,
    Guid SourceStoreId,
    Guid DestinationStoreId,
    Guid ProductId,
    int Quantity,
    TransferStatus Status,
    DateTimeOffset OccurredAt);
