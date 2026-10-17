using RetailStock.Application.Queries;

namespace RetailStock.Api.Endpoints;

internal static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Referentiel").WithOpenApi();

        group.MapGet("/stores", (IStockQueries queries, CancellationToken ct) => queries.GetStoresAsync(ct))
            .WithSummary("Liste des magasins");

        group.MapGet("/products", (IStockQueries queries, CancellationToken ct) => queries.GetProductsAsync(ct))
            .WithSummary("Liste des produits");

        return app;
    }
}
