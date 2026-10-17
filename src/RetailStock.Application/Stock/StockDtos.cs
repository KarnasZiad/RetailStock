using RetailStock.Domain.Stock;

namespace RetailStock.Application.Stock;

public sealed record StockItemDto(
    Guid Id,
    Guid StoreId,
    Guid ProductId,
    int OnHand,
    int Reserved,
    int Available,
    int AlertThreshold,
    StockLevel Level,
    DateTimeOffset UpdatedAt)
{
    public static StockItemDto From(StockItem item) => new(
        item.Id,
        item.StoreId,
        item.ProductId,
        item.OnHand,
        item.Reserved,
        item.Available,
        item.AlertThreshold,
        item.Level,
        item.UpdatedAt);
}

public sealed record ReservationDto(
    Guid Id,
    Guid StoreId,
    Guid ProductId,
    int Quantity,
    string Reference,
    ReservationStatus Status,
    DateTimeOffset ExpiresAt,
    StockItemDto Stock)
{
    public static ReservationDto From(StockItem item, Reservation reservation) => new(
        reservation.Id,
        item.StoreId,
        item.ProductId,
        reservation.Quantity,
        reservation.Reference,
        reservation.Status,
        reservation.ExpiresAt,
        StockItemDto.From(item));
}
