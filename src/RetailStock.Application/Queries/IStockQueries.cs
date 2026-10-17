using RetailStock.Application.Stock;
using RetailStock.Application.Transfers;
using RetailStock.Domain.Stock;
using RetailStock.Domain.Transfers;

namespace RetailStock.Application.Queries;

public sealed record StoreDto(Guid Id, string Code, string Name, string City);

public sealed record ProductDto(Guid Id, string Sku, string Name, string Category, decimal Price);

public sealed record MovementDto(
    Guid Id,
    Guid StoreId,
    Guid ProductId,
    MovementType Type,
    int OnHandDelta,
    int ReservedDelta,
    int OnHandAfter,
    int AvailableAfter,
    string? Reference,
    DateTimeOffset OccurredAt);

/// <summary>Read side: optimised, no-tracking projections.</summary>
public interface IStockQueries
{
    Task<IReadOnlyList<StoreDto>> GetStoresAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockItemDto>> GetStockAsync(Guid? storeId, Guid? productId, CancellationToken cancellationToken = default);

    /// <summary>Stock lines that are low or out of stock.</summary>
    Task<IReadOnlyList<StockItemDto>> GetAlertsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovementDto>> GetMovementsAsync(Guid? storeId, Guid? productId, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransferDto>> GetTransfersAsync(TransferStatus? status, CancellationToken cancellationToken = default);
}
