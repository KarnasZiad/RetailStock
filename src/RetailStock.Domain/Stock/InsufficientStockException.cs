using RetailStock.Domain.Common;

namespace RetailStock.Domain.Stock;

public sealed class InsufficientStockException(Guid storeId, Guid productId, int requested, int available)
    : DomainException(
        StockErrors.Insufficient,
        $"Insufficient stock: {requested} requested but only {available} available.")
{
    public Guid StoreId { get; } = storeId;

    public Guid ProductId { get; } = productId;

    public int Requested { get; } = requested;

    public int Available { get; } = available;
}
