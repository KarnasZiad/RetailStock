using RetailStock.Application.Abstractions;
using RetailStock.Application.Common;
using RetailStock.Application.Stock;
using RetailStock.Domain.Transfers;

namespace RetailStock.Application.Transfers;

/// <summary>Inter-store transfers: ship from the source store, receive at the destination or cancel.</summary>
public sealed class TransferService(
    IStockRepository stock,
    ITransferRepository transfers,
    IReferenceDataRepository referenceData,
    OperationRunner runner,
    TimeProvider clock)
{
    private DateTimeOffset Now => clock.GetUtcNow();

    public Task<TransferDto> RequestAsync(RequestTransfer command, CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            async context =>
            {
                var transfer = context.Track(Transfer.Request(
                    command.SourceStoreId, command.DestinationStoreId, command.ProductId, command.Quantity, command.Note, Now));

                await referenceData.EnsureExistsAsync(command.DestinationStoreId, command.ProductId, cancellationToken);
                var source = context.Track(await stock.GetExistingAsync(referenceData, command.SourceStoreId, command.ProductId, cancellationToken));

                source.ShipTransfer(command.Quantity, transfer.Id, Now);
                transfers.Add(transfer);
                return TransferDto.From(transfer);
            },
            cancellationToken);

    public Task<TransferDto> ReceiveAsync(Guid transferId, CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            async context =>
            {
                var transfer = context.Track(await LoadAsync(transferId, cancellationToken));
                transfer.MarkReceived(Now);

                var destination = context.Track(await stock.GetOrCreateAsync(
                    referenceData, transfer.DestinationStoreId, transfer.ProductId, Now, cancellationToken));
                destination.ReceiveTransfer(transfer.Quantity, transfer.Id, Now);
                return TransferDto.From(transfer);
            },
            cancellationToken);

    public Task<TransferDto> CancelAsync(Guid transferId, CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            async context =>
            {
                var transfer = context.Track(await LoadAsync(transferId, cancellationToken));
                transfer.Cancel(Now);

                var source = context.Track(await stock.GetOrCreateAsync(
                    referenceData, transfer.SourceStoreId, transfer.ProductId, Now, cancellationToken));
                source.ReturnTransfer(transfer.Quantity, transfer.Id, Now);
                return TransferDto.From(transfer);
            },
            cancellationToken);

    private async Task<Transfer> LoadAsync(Guid transferId, CancellationToken cancellationToken) =>
        await transfers.GetAsync(transferId, cancellationToken)
        ?? throw new NotFoundException("Transfer", transferId);
}
