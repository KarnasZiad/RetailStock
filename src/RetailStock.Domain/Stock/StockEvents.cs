using RetailStock.Domain.Common;

namespace RetailStock.Domain.Stock;

/// <summary>Raised for every quantity change. Persisted as a <see cref="StockMovement"/> and pushed in real time.</summary>
public sealed record StockChanged(
    Guid StockItemId,
    Guid StoreId,
    Guid ProductId,
    MovementType Type,
    int OnHandDelta,
    int ReservedDelta,
    int OnHand,
    int Reserved,
    int Available,
    StockLevel Level,
    string? Reference,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>The available quantity crossed the alert threshold (low stock) or reached zero (out of stock).</summary>
public sealed record StockAlertRaised(
    Guid StockItemId,
    Guid StoreId,
    Guid ProductId,
    StockLevel Level,
    int Available,
    int Threshold,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>The available quantity went back above the alert threshold.</summary>
public sealed record StockAlertCleared(
    Guid StockItemId,
    Guid StoreId,
    Guid ProductId,
    int Available,
    int Threshold,
    DateTimeOffset OccurredAt) : IDomainEvent;
