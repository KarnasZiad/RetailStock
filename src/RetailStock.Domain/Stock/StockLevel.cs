namespace RetailStock.Domain.Stock;

public enum StockLevel
{
    Ok,
    Low,
    OutOfStock,
}

public static class StockLevels
{
    public static StockLevel Evaluate(int available, int alertThreshold) => available switch
    {
        <= 0 => StockLevel.OutOfStock,
        _ when available <= alertThreshold => StockLevel.Low,
        _ => StockLevel.Ok,
    };
}
