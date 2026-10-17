using RetailStock.Application.Abstractions;
using RetailStock.Application.Common;
using RetailStock.Domain.Stock;

namespace RetailStock.Application.Stock;

/// <summary>Use cases of the store stock: receipts, sales, inventory counts and reservations.</summary>
public sealed class StockService(
    IStockRepository stock,
    IReferenceDataRepository referenceData,
    OperationRunner runner,
    TimeProvider clock)
{
    public static readonly TimeSpan DefaultReservationTimeToLive = TimeSpan.FromHours(48);

    private DateTimeOffset Now => clock.GetUtcNow();

    public Task<StockItemDto> ReceiveAsync(ReceiveStock command, CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            async context =>
            {
                var item = context.Track(await stock.GetOrCreateAsync(referenceData, command.StoreId, command.ProductId, Now, cancellationToken));
                item.Receive(command.Quantity, command.Reference, Now);
                return StockItemDto.From(item);
            },
            cancellationToken);

    public Task<StockItemDto> SellAsync(RecordSale command, CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            async context =>
            {
                var item = context.Track(await stock.GetExistingAsync(referenceData, command.StoreId, command.ProductId, cancellationToken));
                item.Sell(command.Quantity, command.Reference, Now);
                return StockItemDto.From(item);
            },
            cancellationToken);

    public Task<StockItemDto> AdjustAsync(AdjustStock command, CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            async context =>
            {
                var item = context.Track(await stock.GetOrCreateAsync(referenceData, command.StoreId, command.ProductId, Now, cancellationToken));
                item.Adjust(command.CountedQuantity, command.Reason, Now);
                return StockItemDto.From(item);
            },
            cancellationToken);

    public Task<ReservationDto> ReserveAsync(ReserveStock command, CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            async context =>
            {
                var item = context.Track(await stock.GetExistingAsync(referenceData, command.StoreId, command.ProductId, cancellationToken));
                var expiresAt = Now + (command.TimeToLive ?? DefaultReservationTimeToLive);
                var reservation = item.Reserve(command.Quantity, command.Reference, expiresAt, Now);
                return ReservationDto.From(item, reservation);
            },
            cancellationToken);

    public Task<StockItemDto> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            async context =>
            {
                var item = context.Track(await LoadByReservationAsync(reservationId, cancellationToken));
                item.ConfirmReservation(reservationId, Now);
                return StockItemDto.From(item);
            },
            cancellationToken);

    public Task<StockItemDto> ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default) =>
        runner.RunAsync(
            async context =>
            {
                var item = context.Track(await LoadByReservationAsync(reservationId, cancellationToken));
                item.ReleaseReservation(reservationId, Now);
                return StockItemDto.From(item);
            },
            cancellationToken);

    /// <summary>Releases every reservation past its expiry date. Called periodically by a background worker.</summary>
    public async Task<int> ExpireReservationsAsync(CancellationToken cancellationToken = default)
    {
        var stockItemIds = await stock.GetIdsWithExpiredReservationsAsync(Now, cancellationToken);
        var expired = 0;
        foreach (var stockItemId in stockItemIds)
        {
            expired += await runner.RunAsync(
                async context =>
                {
                    var item = await stock.GetByIdAsync(stockItemId, cancellationToken);
                    return item is null ? 0 : context.Track(item).ExpireReservations(Now);
                },
                cancellationToken);
        }

        return expired;
    }

    private async Task<StockItem> LoadByReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
        await stock.GetByReservationAsync(reservationId, cancellationToken)
        ?? throw new NotFoundException("Reservation", reservationId);
}
