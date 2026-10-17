using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Queries;
using RetailStock.Application.Stock;
using RetailStock.Application.Transfers;
using RetailStock.Domain.Stock;
using RetailStock.Domain.Transfers;

namespace RetailStock.Infrastructure.Persistence.Queries;

internal sealed class StockQueries(RetailStockDbContext db) : IStockQueries
{
    public async Task<IReadOnlyList<StoreDto>> GetStoresAsync(CancellationToken cancellationToken = default) =>
        await db.Stores.AsNoTracking()
            .OrderBy(s => s.Code)
            .Select(s => new StoreDto(s.Id, s.Code, s.Name, s.City))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        await db.Products.AsNoTracking()
            .OrderBy(p => p.Sku)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Category, p.Price))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StockItemDto>> GetStockAsync(Guid? storeId, Guid? productId, CancellationToken cancellationToken = default)
    {
        var query = db.StockItems.AsNoTracking();
        if (storeId is not null)
        {
            query = query.Where(s => s.StoreId == storeId);
        }

        if (productId is not null)
        {
            query = query.Where(s => s.ProductId == productId);
        }

        return await ProjectAsync(query, cancellationToken);
    }

    public Task<IReadOnlyList<StockItemDto>> GetAlertsAsync(CancellationToken cancellationToken = default) =>
        ProjectAsync(
            db.StockItems.AsNoTracking().Where(s => s.OnHand - s.Reserved <= s.AlertThreshold),
            cancellationToken);

    public async Task<IReadOnlyList<MovementDto>> GetMovementsAsync(Guid? storeId, Guid? productId, int take, CancellationToken cancellationToken = default)
    {
        var query = db.StockMovements.AsNoTracking();
        if (storeId is not null)
        {
            query = query.Where(m => m.StoreId == storeId);
        }

        if (productId is not null)
        {
            query = query.Where(m => m.ProductId == productId);
        }

        return await query
            .OrderByDescending(m => m.OccurredAt)
            .Take(Math.Clamp(take, 1, 500))
            .Select(m => new MovementDto(
                m.Id, m.StoreId, m.ProductId, m.Type, m.OnHandDelta, m.ReservedDelta, m.OnHandAfter, m.AvailableAfter, m.Reference, m.OccurredAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TransferDto>> GetTransfersAsync(TransferStatus? status, CancellationToken cancellationToken = default)
    {
        var query = db.Transfers.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        return await query
            .OrderByDescending(t => t.RequestedAt)
            .Take(200)
            .Select(t => new TransferDto(
                t.Id, t.SourceStoreId, t.DestinationStoreId, t.ProductId, t.Quantity, t.Status, t.Note, t.RequestedAt, t.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<StockItemDto>> ProjectAsync(IQueryable<StockItem> query, CancellationToken cancellationToken)
    {
        var rows = await query
            .OrderBy(s => s.StoreId).ThenBy(s => s.ProductId)
            .Select(s => new { s.Id, s.StoreId, s.ProductId, s.OnHand, s.Reserved, s.AlertThreshold, s.UpdatedAt })
            .ToListAsync(cancellationToken);

        return rows
            .Select(s => new StockItemDto(
                s.Id,
                s.StoreId,
                s.ProductId,
                s.OnHand,
                s.Reserved,
                s.OnHand - s.Reserved,
                s.AlertThreshold,
                StockLevels.Evaluate(s.OnHand - s.Reserved, s.AlertThreshold),
                s.UpdatedAt))
            .ToList();
    }
}
