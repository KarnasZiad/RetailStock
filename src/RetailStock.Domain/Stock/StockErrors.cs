namespace RetailStock.Domain.Stock;

/// <summary>Stable error codes returned to API clients.</summary>
public static class StockErrors
{
    public const string InvalidQuantity = "stock.invalid_quantity";
    public const string InvalidThreshold = "stock.invalid_threshold";
    public const string Insufficient = "stock.insufficient";
    public const string NegativeStock = "stock.negative";
    public const string BelowReserved = "stock.below_reserved";
    public const string ReferenceRequired = "reservation.reference_required";
    public const string InvalidExpiry = "reservation.invalid_expiry";
    public const string ReservationNotFound = "reservation.not_found";
    public const string ReservationNotActive = "reservation.not_active";
    public const string ReservationExpired = "reservation.expired";
    public const string SameStore = "transfer.same_store";
    public const string TransferNotInTransit = "transfer.not_in_transit";
}
