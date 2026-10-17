namespace RetailStock.Application.Stock;

public sealed record ReceiveStock(Guid StoreId, Guid ProductId, int Quantity, string? Reference);

public sealed record RecordSale(Guid StoreId, Guid ProductId, int Quantity, string? Reference);

public sealed record AdjustStock(Guid StoreId, Guid ProductId, int CountedQuantity, string? Reason);

public sealed record ReserveStock(Guid StoreId, Guid ProductId, int Quantity, string Reference, TimeSpan? TimeToLive = null);
