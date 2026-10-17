using RetailStock.Domain.Transfers;

namespace RetailStock.Application.Abstractions;

public interface ITransferRepository
{
    Task<Transfer?> GetAsync(Guid transferId, CancellationToken cancellationToken = default);

    void Add(Transfer transfer);
}
