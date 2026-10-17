using Microsoft.AspNetCore.Http.HttpResults;
using RetailStock.Application.Queries;
using RetailStock.Application.Transfers;
using RetailStock.Domain.Transfers;

namespace RetailStock.Api.Endpoints;

internal static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transfers").WithTags("Transferts").WithOpenApi();

        group.MapGet("/", (TransferStatus? status, IStockQueries queries, CancellationToken ct) => queries.GetTransfersAsync(status, ct))
            .WithSummary("Transferts inter-magasins");

        group.MapPost("/", async Task<Created<TransferDto>> (TransferRequest request, TransferService service, CancellationToken ct) =>
            {
                var transfer = await service.RequestAsync(
                    new RequestTransfer(request.SourceStoreId, request.DestinationStoreId, request.ProductId, request.Quantity, request.Note), ct);
                return TypedResults.Created($"/api/transfers/{transfer.Id}", transfer);
            })
            .WithSummary("Expedition d'un transfert : le stock quitte le magasin source")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{transferId:guid}/receive", async (Guid transferId, TransferService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ReceiveAsync(transferId, ct)))
            .WithSummary("Reception du transfert par le magasin destinataire")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{transferId:guid}/cancel", async (Guid transferId, TransferService service, CancellationToken ct) =>
                TypedResults.Ok(await service.CancelAsync(transferId, ct)))
            .WithSummary("Annulation : le stock retourne au magasin source")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}
