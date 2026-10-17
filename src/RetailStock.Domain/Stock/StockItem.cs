using RetailStock.Domain.Common;

namespace RetailStock.Domain.Stock;

/// <summary>
/// Stock of one product in one store. Aggregate root guarding the core invariants:
/// <list type="bullet">
///   <item>on-hand and reserved quantities are never negative;</item>
///   <item>reserved quantity never exceeds on-hand quantity (available = on-hand - reserved &gt;= 0);</item>
///   <item>a sale, a transfer or a reservation can only consume the <em>available</em> quantity.</item>
/// </list>
/// </summary>
public sealed class StockItem : AggregateRoot
{
    public const int DefaultAlertThreshold = 5;

    private readonly List<Reservation> _reservations = [];

    private StockItem()
    {
    }

    public StockItem(Guid storeId, Guid productId, DateTimeOffset createdAt, int alertThreshold = DefaultAlertThreshold)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store id is required.", nameof(storeId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (alertThreshold < 0)
        {
            throw new DomainException(StockErrors.InvalidThreshold, "Alert threshold cannot be negative.");
        }

        StoreId = storeId;
        ProductId = productId;
        AlertThreshold = alertThreshold;
        UpdatedAt = createdAt;
    }

    public Guid StoreId { get; private set; }

    public Guid ProductId { get; private set; }

    public int OnHand { get; private set; }

    public int Reserved { get; private set; }

    public int Available => OnHand - Reserved;

    public int AlertThreshold { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Optimistic concurrency token (mapped to PostgreSQL <c>xmin</c>).</summary>
    public uint Version { get; private set; }

    public IReadOnlyCollection<Reservation> Reservations => _reservations;

    public StockLevel Level => StockLevels.Evaluate(Available, AlertThreshold);

    public void Receive(int quantity, string? reference, DateTimeOffset now)
    {
        EnsurePositive(quantity);
        Apply(MovementType.Receipt, quantity, 0, reference, now);
    }

    public void Sell(int quantity, string? reference, DateTimeOffset now)
    {
        EnsurePositive(quantity);
        EnsureAvailable(quantity);
        Apply(MovementType.Sale, -quantity, 0, reference, now);
    }

    /// <summary>Inventory count: sets the on-hand quantity to what was physically counted.</summary>
    public void Adjust(int countedQuantity, string? reason, DateTimeOffset now)
    {
        if (countedQuantity < 0)
        {
            throw new DomainException(StockErrors.NegativeStock, "Counted quantity cannot be negative.");
        }

        if (countedQuantity < Reserved)
        {
            throw new DomainException(
                StockErrors.BelowReserved,
                $"Counted quantity ({countedQuantity}) is lower than the reserved quantity ({Reserved}). Release reservations first.");
        }

        var delta = countedQuantity - OnHand;
        if (delta != 0)
        {
            Apply(MovementType.Adjustment, delta, 0, reason, now);
        }
    }

    public void ShipTransfer(int quantity, Guid transferId, DateTimeOffset now)
    {
        EnsurePositive(quantity);
        EnsureAvailable(quantity);
        Apply(MovementType.TransferOut, -quantity, 0, transferId.ToString(), now);
    }

    public void ReceiveTransfer(int quantity, Guid transferId, DateTimeOffset now)
    {
        EnsurePositive(quantity);
        Apply(MovementType.TransferIn, quantity, 0, transferId.ToString(), now);
    }

    public void ReturnTransfer(int quantity, Guid transferId, DateTimeOffset now)
    {
        EnsurePositive(quantity);
        Apply(MovementType.TransferReturn, quantity, 0, transferId.ToString(), now);
    }

    public Reservation Reserve(int quantity, string reference, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        EnsurePositive(quantity);

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException(StockErrors.ReferenceRequired, "A reservation requires a reference (order number, customer...).");
        }

        if (expiresAt <= now)
        {
            throw new DomainException(StockErrors.InvalidExpiry, "Reservation expiry must be in the future.");
        }

        EnsureAvailable(quantity);

        var reservation = new Reservation(Id, quantity, reference, now, expiresAt);
        _reservations.Add(reservation);
        Apply(MovementType.Reservation, 0, quantity, reservation.Reference, now);
        return reservation;
    }

    /// <summary>The customer picked the goods up: the reserved quantity leaves the store.</summary>
    public void ConfirmReservation(Guid reservationId, DateTimeOffset now)
    {
        var reservation = FindActiveReservation(reservationId);
        reservation.Confirm(now);
        Apply(MovementType.ReservationConfirmed, -reservation.Quantity, -reservation.Quantity, reservation.Reference, now);
    }

    public void ReleaseReservation(Guid reservationId, DateTimeOffset now)
    {
        var reservation = FindActiveReservation(reservationId);
        reservation.Release(now);
        Apply(MovementType.ReservationReleased, 0, -reservation.Quantity, reservation.Reference, now);
    }

    /// <returns>The number of reservations that expired.</returns>
    public int ExpireReservations(DateTimeOffset now)
    {
        var expired = _reservations.Where(r => r.IsExpired(now)).ToList();
        foreach (var reservation in expired)
        {
            reservation.Expire(now);
            Apply(MovementType.ReservationExpired, 0, -reservation.Quantity, reservation.Reference, now);
        }

        return expired.Count;
    }

    private Reservation FindActiveReservation(Guid reservationId)
    {
        var reservation = _reservations.SingleOrDefault(r => r.Id == reservationId)
            ?? throw new DomainException(StockErrors.ReservationNotFound, $"Reservation '{reservationId}' does not belong to this stock item.");

        if (reservation.Status != ReservationStatus.Active)
        {
            throw new DomainException(StockErrors.ReservationNotActive, $"Reservation '{reservation.Reference}' is already {reservation.Status}.");
        }

        return reservation;
    }

    private static void EnsurePositive(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException(StockErrors.InvalidQuantity, "Quantity must be strictly positive.");
        }
    }

    private void EnsureAvailable(int quantity)
    {
        if (quantity > Available)
        {
            throw new InsufficientStockException(StoreId, ProductId, quantity, Available);
        }
    }

    private void Apply(MovementType type, int onHandDelta, int reservedDelta, string? reference, DateTimeOffset now)
    {
        var newOnHand = OnHand + onHandDelta;
        var newReserved = Reserved + reservedDelta;

        // Last line of defence: whatever the operation, the invariants must hold.
        if (newOnHand < 0 || newReserved < 0 || newReserved > newOnHand)
        {
            throw new DomainException(StockErrors.NegativeStock, "The operation would result in a negative available stock.");
        }

        var previousLevel = Level;
        OnHand = newOnHand;
        Reserved = newReserved;
        UpdatedAt = now;

        Raise(new StockChanged(Id, StoreId, ProductId, type, onHandDelta, reservedDelta, OnHand, Reserved, Available, Level, reference, now));

        var level = Level;
        if (level == previousLevel)
        {
            return;
        }

        Raise(level == StockLevel.Ok
            ? new StockAlertCleared(Id, StoreId, ProductId, Available, AlertThreshold, now)
            : new StockAlertRaised(Id, StoreId, ProductId, level, Available, AlertThreshold, now));
    }
}
