using MediatR;
using SelectStoreAR.Application.Queries.Products;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this WebApplication app)
    {
        app.MapGet("/api/products/search", SearchProducts)
            .WithTags("Products")
            .WithName("SearchProducts")
            .Produces<SearchProductsResult>()
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> SearchProducts(
        ISender sender,
        string q,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.UnprocessableEntity(new { errors = new { q = new[] { "El término de búsqueda es requerido" } } });
        }

        SearchProductsResult result = await sender.Send(
            new SearchProductsQuery(q, page, pageSize),
            cancellationToken);

        return Results.Ok(result);
    }
}
