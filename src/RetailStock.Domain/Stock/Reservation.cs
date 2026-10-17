using RetailStock.Domain.Common;

namespace RetailStock.Domain.Stock;

/// <summary>
/// Quantity set aside for a customer (click and collect, e-reservation) until it is
/// confirmed (picked up), released or expired. Owned by <see cref="StockItem"/>.
/// </summary>
public sealed class Reservation : Entity
{
    private Reservation()
    {
    }

    internal Reservation(Guid stockItemId, int quantity, string reference, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        StockItemId = stockItemId;
        Quantity = quantity;
        Reference = reference.Trim();
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        Status = ReservationStatus.Active;
    }

    public Guid StockItemId { get; private set; }

    public int Quantity { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public bool IsExpired(DateTimeOffset now) => Status == ReservationStatus.Active && now >= ExpiresAt;

    internal void Confirm(DateTimeOffset now)
    {
        if (IsExpired(now))
        {
            throw new DomainException(StockErrors.ReservationExpired, $"Reservation '{Reference}' expired at {ExpiresAt:O}.");
        }

        Close(ReservationStatus.Confirmed, now);
    }

    internal void Release(DateTimeOffset now) => Close(ReservationStatus.Released, now);

    internal void Expire(DateTimeOffset now) => Close(ReservationStatus.Expired, now);

    private void Close(ReservationStatus status, DateTimeOffset now)
    {
        Status = status;
        ClosedAt = now;
    }
}
