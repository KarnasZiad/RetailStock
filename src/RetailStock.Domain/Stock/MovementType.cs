namespace RetailStock.Domain.Stock;

public enum MovementType
{
    Receipt,
    Sale,
    Adjustment,
    TransferOut,
    TransferIn,
    TransferReturn,
    Reservation,
    ReservationConfirmed,
    ReservationReleased,
    ReservationExpired,
}
