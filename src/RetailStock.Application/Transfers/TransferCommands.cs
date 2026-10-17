using RetailStock.Domain.Transfers;

namespace RetailStock.Application.Transfers;

public sealed record RequestTransfer(Guid SourceStoreId, Guid DestinationStoreId, Guid ProductId, int Quantity, string? Note);

public sealed record TransferDto(
    Guid Id,
    Guid SourceStoreId,
    Guid DestinationStoreId,
    Guid ProductId,
    int Quantity,
    TransferStatus Status,
    string? Note,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt)
{
    public static TransferDto From(Transfer transfer) => new(
        transfer.Id,
        transfer.SourceStoreId,
        transfer.DestinationStoreId,
        transfer.ProductId,
        transfer.Quantity,
        transfer.Status,
        transfer.Note,
        transfer.RequestedAt,
        transfer.CompletedAt);
}
