namespace RetailStock.Api.Endpoints;

/// <summary>Quantity received or sold.</summary>
/// <param name="Quantity">Strictly positive quantity.</param>
/// <param name="Reference">Delivery note, receipt number...</param>
public sealed record QuantityRequest(int Quantity, string? Reference);

/// <summary>Physical inventory count.</summary>
/// <param name="CountedQuantity">Quantity physically counted in store.</param>
/// <param name="Reason">Why the stock is adjusted (inventory, damaged goods...).</param>
public sealed record AdjustmentRequest(int CountedQuantity, string? Reason);

/// <summary>Reservation for a customer (click and collect, e-reservation).</summary>
/// <param name="Quantity">Quantity to set aside.</param>
/// <param name="Reference">Order number or customer reference.</param>
/// <param name="TtlMinutes">Lifetime of the reservation, 48 hours by default.</param>
public sealed record ReservationRequest(int Quantity, string Reference, int? TtlMinutes);

/// <summary>Inter-store transfer request.</summary>
public sealed record TransferRequest(Guid SourceStoreId, Guid DestinationStoreId, Guid ProductId, int Quantity, string? Note);
