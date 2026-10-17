namespace RetailStock.Api.Realtime;

/// <summary>Messages pushed by the server to the dashboards.</summary>
public interface IStockClient
{
    Task StockChanged(StockChangedMessage message);

    Task StockAlert(StockAlertMessage message);

    Task TransferUpdated(TransferUpdatedMessage message);
}
