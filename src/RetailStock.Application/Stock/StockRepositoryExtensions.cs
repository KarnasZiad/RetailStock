using RetailStock.Application.Abstractions;
using RetailStock.Application.Common;
using RetailStock.Domain.Stock;

namespace RetailStock.Application.Stock;

internal static class StockRepositoryExtensions
{
    /// <summary>Returns the stock line, creating it (with zero quantity) the first time a product enters a store.</summary>
    public static async Task<StockItem> GetOrCreateAsync(
        this IStockRepository stock,
        IReferenceDataRepository referenceData,
        Guid storeId,
        Guid productId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var item = await stock.GetAsync(storeId, productId, cancellationToken);
        if (item is not null)
        {
            return item;
        }

        await referenceData.EnsureExistsAsync(storeId, productId, cancellationToken);
        item = new StockItem(storeId, productId, now);
        stock.Add(item);
        return item;
    }

    public static async Task<StockItem> GetExistingAsync(
        this IStockRepository stock,
        IReferenceDataRepository referenceData,
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var item = await stock.GetAsync(storeId, productId, cancellationToken);
        if (item is not null)
        {
            return item;
        }

        // Unknown store or product: 404. Known but never stocked: nothing is available.
        await referenceData.EnsureExistsAsync(storeId, productId, cancellationToken);
        throw new InsufficientStockException(storeId, productId, 1, 0);
    }

    public static async Task EnsureExistsAsync(
        this IReferenceDataRepository referenceData,
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await referenceData.StoreExistsAsync(storeId, cancellationToken))
        {
            throw new NotFoundException("Store", storeId);
        }

        if (!await referenceData.ProductExistsAsync(productId, cancellationToken))
        {
            throw new NotFoundException("Product", productId);
        }
    }
}
