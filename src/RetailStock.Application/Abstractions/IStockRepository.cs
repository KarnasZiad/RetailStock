using RetailStock.Domain.Stock;

namespace RetailStock.Application.Abstractions;

public interface IStockRepository
{
    /// <summary>Loads the stock item with its active reservations.</summary>
    Task<StockItem?> GetAsync(Guid storeId, Guid productId, CancellationToken cancellationToken = default);

    Task<StockItem?> GetByIdAsync(Guid stockItemId, CancellationToken cancellationToken = default);

    /// <summary>Loads the stock item owning the reservation (the reservation is loaded whatever its status).</summary>
    Task<StockItem?> GetByReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetIdsWithExpiredReservationsAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    void Add(StockItem stockItem);

    void AddMovements(IEnumerable<StockMovement> movements);
}
