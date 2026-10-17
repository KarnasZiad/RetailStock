using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Abstractions;
using RetailStock.Domain.Transfers;

namespace RetailStock.Infrastructure.Persistence.Repositories;

internal sealed class TransferRepository(RetailStockDbContext db) : ITransferRepository
{
    public Task<Transfer?> GetAsync(Guid transferId, CancellationToken cancellationToken = default) =>
        db.Transfers.SingleOrDefaultAsync(t => t.Id == transferId, cancellationToken);

    public void Add(Transfer transfer) => db.Transfers.Add(transfer);
}
