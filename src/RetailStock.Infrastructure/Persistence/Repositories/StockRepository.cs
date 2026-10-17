using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Abstractions;
using RetailStock.Domain.Stock;

namespace RetailStock.Infrastructure.Persistence.Repositories;

internal sealed class StockRepository(RetailStockDbContext db) : IStockRepository
{
    public Task<StockItem?> GetAsync(Guid storeId, Guid productId, CancellationToken cancellationToken = default) =>
        db.StockItems
            .Include(s => s.Reservations.Where(r => r.Status == ReservationStatus.Active))
            .SingleOrDefaultAsync(s => s.StoreId == storeId && s.ProductId == productId, cancellationToken);

    public Task<StockItem?> GetByIdAsync(Guid stockItemId, CancellationToken cancellationToken = default) =>
        db.StockItems
            .Include(s => s.Reservations.Where(r => r.Status == ReservationStatus.Active))
            .SingleOrDefaultAsync(s => s.Id == stockItemId, cancellationToken);

    public async Task<StockItem?> GetByReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var stockItemId = await db.Reservations
            .Where(r => r.Id == reservationId)
            .Select(r => (Guid?)r.StockItemId)
            .SingleOrDefaultAsync(cancellationToken);

        if (stockItemId is null)
        {
            return null;
        }

        return await db.StockItems
            .Include(s => s.Reservations.Where(r => r.Status == ReservationStatus.Active || r.Id == reservationId))
            .SingleAsync(s => s.Id == stockItemId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetIdsWithExpiredReservationsAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        await db.Reservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt <= now)
            .Select(r => r.StockItemId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public void Add(StockItem stockItem) => db.StockItems.Add(stockItem);

    public void AddMovements(IEnumerable<StockMovement> movements) => db.StockMovements.AddRange(movements);
}
