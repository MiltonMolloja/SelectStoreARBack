using MediatR;
using SelectStoreAR.Application.Commands.Products;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Queries.Products;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", GetProducts)
            .WithName("GetProducts")
            .Produces<PagedResult<ProductDto>>();

        group.MapGet("/{slug}", GetProductBySlug)
            .WithName("GetProductBySlug")
            .Produces<ProductDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .RequireAuthorization("admin")
            .Produces<ProductDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}", UpdateProduct)
            .WithName("UpdateProduct")
            .RequireAuthorization("admin")
            .Produces<ProductDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteProduct)
            .WithName("DeleteProduct")
            .RequireAuthorization("admin")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetProducts(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        Guid? categoryId = null,
        string? status = null,
        string? q = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        PagedResult<ProductDto> result = await sender.Send(
            new GetProductsQuery(page, pageSize, categoryId, status, q, minPrice, maxPrice),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetProductBySlug(
        string slug,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ProductDto? product = await sender.Send(new GetProductBySlugQuery(slug), cancellationToken);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> CreateProduct(
        CreateProductCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ProductDto product = await sender.Send(command, cancellationToken);
        return Results.Created($"/api/products/{product.Slug}", product);
    }

    private static async Task<IResult> UpdateProduct(
        Guid id,
        UpdateProductCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ProductDto product = await sender.Send(command with { Id = id }, cancellationToken);
        return Results.Ok(product);
    }

    private static async Task<IResult> DeleteProduct(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(id), cancellationToken);
        return Results.NoContent();
    }
}
