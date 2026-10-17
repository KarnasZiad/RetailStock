using RetailStock.Domain.Common;
using RetailStock.Domain.Stock;

namespace RetailStock.Domain.Transfers;

/// <summary>
/// Inter-store transfer. Goods leave the source store when the transfer is requested
/// (status <see cref="TransferStatus.InTransit"/>), then are either received by the
/// destination store or returned to the source when the transfer is cancelled.
/// </summary>
public sealed class Transfer : AggregateRoot
{
    private Transfer()
    {
    }

    public Guid SourceStoreId { get; private set; }

    public Guid DestinationStoreId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public TransferStatus Status { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public uint Version { get; private set; }

    public static Transfer Request(Guid sourceStoreId, Guid destinationStoreId, Guid productId, int quantity, string? note, DateTimeOffset now)
    {
        if (sourceStoreId == destinationStoreId)
        {
            throw new DomainException(StockErrors.SameStore, "Source and destination stores must be different.");
        }

        if (quantity <= 0)
        {
            throw new DomainException(StockErrors.InvalidQuantity, "Quantity must be strictly positive.");
        }

        var transfer = new Transfer
        {
            SourceStoreId = sourceStoreId,
            DestinationStoreId = destinationStoreId,
            ProductId = productId,
            Quantity = quantity,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Status = TransferStatus.InTransit,
            RequestedAt = now,
        };
        transfer.RaiseStatusChanged(now);
        return transfer;
    }

    public void MarkReceived(DateTimeOffset now) => Complete(TransferStatus.Received, now);

    public void Cancel(DateTimeOffset now) => Complete(TransferStatus.Cancelled, now);

    private void Complete(TransferStatus status, DateTimeOffset now)
    {
        if (Status != TransferStatus.InTransit)
        {
            throw new DomainException(StockErrors.TransferNotInTransit, $"Transfer is already {Status}.");
        }

        Status = status;
        CompletedAt = now;
        RaiseStatusChanged(now);
    }

    private void RaiseStatusChanged(DateTimeOffset now) =>
        Raise(new TransferStatusChanged(Id, SourceStoreId, DestinationStoreId, ProductId, Quantity, Status, now));
}
