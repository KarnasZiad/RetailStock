namespace RetailStock.Application.Abstractions;

public interface IReferenceDataRepository
{
    Task<bool> StoreExistsAsync(Guid storeId, CancellationToken cancellationToken = default);

    Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default);
}
