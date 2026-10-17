using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Abstractions;

namespace RetailStock.Infrastructure.Persistence.Repositories;

internal sealed class ReferenceDataRepository(RetailStockDbContext db) : IReferenceDataRepository
{
    public Task<bool> StoreExistsAsync(Guid storeId, CancellationToken cancellationToken = default) =>
        db.Stores.AnyAsync(s => s.Id == storeId, cancellationToken);

    public Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default) =>
        db.Products.AnyAsync(p => p.Id == productId, cancellationToken);
}
