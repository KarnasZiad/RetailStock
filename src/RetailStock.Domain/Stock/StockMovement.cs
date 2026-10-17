using RetailStock.Domain.Common;

namespace RetailStock.Domain.Stock;

/// <summary>Immutable ledger entry: the audit trail of every stock change.</summary>
public sealed class StockMovement : Entity
{
    private StockMovement()
    {
    }

    public Guid StockItemId { get; private set; }

    public Guid StoreId { get; private set; }

    public Guid ProductId { get; private set; }

    public MovementType Type { get; private set; }

    public int OnHandDelta { get; private set; }

    public int ReservedDelta { get; private set; }

    public int OnHandAfter { get; private set; }

    public int AvailableAfter { get; private set; }

    public string? Reference { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static StockMovement From(StockChanged change) => new()
    {
        Id = Guid.NewGuid(),
        StockItemId = change.StockItemId,
        StoreId = change.StoreId,
        ProductId = change.ProductId,
        Type = change.Type,
        OnHandDelta = change.OnHandDelta,
        ReservedDelta = change.ReservedDelta,
        OnHandAfter = change.OnHand,
        AvailableAfter = change.Available,
        Reference = change.Reference,
        OccurredAt = change.OccurredAt,
    };
}
