using Microsoft.AspNetCore.Http.HttpResults;
using RetailStock.Application.Queries;
using RetailStock.Application.Stock;

namespace RetailStock.Api.Endpoints;

internal static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Stock").WithOpenApi();

        api.MapGet("/stock", (Guid? storeId, Guid? productId, IStockQueries queries, CancellationToken ct) =>
                queries.GetStockAsync(storeId, productId, ct))
            .WithSummary("Etat du stock, filtrable par magasin et/ou produit");

        api.MapGet("/stock/alerts", (IStockQueries queries, CancellationToken ct) => queries.GetAlertsAsync(ct))
            .WithSummary("Lignes de stock en alerte (stock bas ou rupture)");

        api.MapGet("/movements", (Guid? storeId, Guid? productId, int? take, IStockQueries queries, CancellationToken ct) =>
                queries.GetMovementsAsync(storeId, productId, take ?? 50, ct))
            .WithSummary("Journal des mouvements de stock (le plus recent en premier)");

        var line = api.MapGroup("/stores/{storeId:guid}/products/{productId:guid}");

        line.MapPost("/receipts", async (Guid storeId, Guid productId, QuantityRequest request, StockService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ReceiveAsync(new ReceiveStock(storeId, productId, request.Quantity, request.Reference), ct)))
            .WithSummary("Reception de marchandise")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        line.MapPost("/sales", async (Guid storeId, Guid productId, QuantityRequest request, StockService service, CancellationToken ct) =>
                TypedResults.Ok(await service.SellAsync(new RecordSale(storeId, productId, request.Quantity, request.Reference), ct)))
            .WithSummary("Vente en caisse (refusee si le stock disponible est insuffisant)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        line.MapPost("/adjustments", async (Guid storeId, Guid productId, AdjustmentRequest request, StockService service, CancellationToken ct) =>
                TypedResults.Ok(await service.AdjustAsync(new AdjustStock(storeId, productId, request.CountedQuantity, request.Reason), ct)))
            .WithSummary("Ajustement d'inventaire (comptage physique)")
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        line.MapPost("/reservations", async Task<Created<ReservationDto>> (Guid storeId, Guid productId, ReservationRequest request, StockService service, CancellationToken ct) =>
            {
                var ttl = request.TtlMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : (TimeSpan?)null;
                var reservation = await service.ReserveAsync(new ReserveStock(storeId, productId, request.Quantity, request.Reference, ttl), ct);
                return TypedResults.Created($"/api/reservations/{reservation.Id}", reservation);
            })
            .WithSummary("Reservation de stock (click and collect, e-reservation)")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        var reservations = api.MapGroup("/reservations/{reservationId:guid}");

        reservations.MapPost("/confirm", async (Guid reservationId, StockService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ConfirmReservationAsync(reservationId, ct)))
            .WithSummary("Retrait client : la quantite reservee sort du stock")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        reservations.MapPost("/release", async (Guid reservationId, StockService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ReleaseReservationAsync(reservationId, ct)))
            .WithSummary("Annulation : la quantite reservee redevient disponible")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}
