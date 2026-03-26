using System.Security.Claims;
using MediatR;
using SelectStoreAR.Application.Commands.Categories;
using SelectStoreAR.Application.Commands.Products;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Interfaces;
using SelectStoreAR.Application.Queries.Admin;
using SelectStoreAR.Application.Queries.Categories;
using SelectStoreAR.Application.Queries.Orders;
using SelectStoreAR.Application.Queries.Products;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        RouteGroupBuilder admin = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("admin");

        // ── Dashboard ──────────────────────────────────────────────────────
        admin.MapGet("/dashboard", GetDashboard)
            .WithName("GetDashboard")
            .Produces<DashboardDto>();

        // ── Products ───────────────────────────────────────────────────────
        admin.MapGet("/products", GetAdminProducts)
            .WithName("GetAdminProducts")
            .Produces<PagedResult<ProductDto>>();

        admin.MapPost("/products/{id:guid}/images", UploadProductImages)
            .WithName("UploadProductImages")
            .Accepts<IFormFileCollection>("multipart/form-data")
            .Produces<IReadOnlyList<ProductImageDto>>(StatusCodes.Status201Created)
            .DisableAntiforgery();

        admin.MapPatch("/products/{id:guid}/status", ChangeProductStatus)
            .WithName("ChangeProductStatus")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        admin.MapPatch("/products/{id:guid}/featured", SetProductFeatured)
            .WithName("SetProductFeatured")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // ── Categories ─────────────────────────────────────────────────────
        admin.MapGet("/categories", GetAdminCategories)
            .WithName("GetAdminCategories")
            .Produces<IReadOnlyList<CategoryDto>>();

        admin.MapPost("/categories", CreateCategory)
            .WithName("CreateCategory")
            .Produces<CategoryDto>(StatusCodes.Status201Created);

        admin.MapPut("/categories/{id:guid}", UpdateCategory)
            .WithName("UpdateCategory")
            .Produces<CategoryDto>()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapDelete("/categories/{id:guid}", DeleteCategory)
            .WithName("DeleteCategory")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // ── Orders ─────────────────────────────────────────────────────────
        admin.MapGet("/orders", GetAdminOrders)
            .WithName("GetAdminOrders")
            .Produces<PagedResult<OrderDto>>();

        admin.MapGet("/orders/{id:guid}", GetAdminOrderById)
            .WithName("GetAdminOrderById")
            .Produces<OrderDto>()
            .Produces(StatusCodes.Status404NotFound);

        // ── Config ─────────────────────────────────────────────────────────
        admin.MapPut("/config/whatsapp", UpdateWhatsApp)
            .WithName("UpdateWhatsApp")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> GetDashboard(ISender sender, CancellationToken cancellationToken)
    {
        DashboardDto dashboard = await sender.Send(new GetDashboardQuery(), cancellationToken);
        return Results.Ok(dashboard);
    }

    private static async Task<IResult> GetAdminProducts(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        Guid? categoryId = null,
        string? status = null,
        string? q = null,
        CancellationToken cancellationToken = default)
    {
        // Admin puede ver cualquier estado, incluyendo borradores
        PagedResult<ProductDto> result = await sender.Send(
            new GetProductsQuery(page, pageSize, categoryId, status, q),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UploadProductImages(
        Guid id,
        IFormFileCollection files,
        ISender sender,
        IImageService imageService,
        CancellationToken cancellationToken)
    {
        List<(Stream, string)> streams = [];
        foreach (IFormFile file in files)
        {
            streams.Add((file.OpenReadStream(), file.FileName));
        }

        IReadOnlyList<ProductImageDto> images = await sender.Send(
            new UploadProductImagesCommand(id, streams),
            cancellationToken);

        return Results.Created($"/api/products/{id}", images);
    }

    private static async Task<IResult> ChangeProductStatus(
        Guid id,
        ChangeStatusRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeProductStatusCommand(id, request.Status), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> SetProductFeatured(
        Guid id,
        SetFeaturedRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetProductFeaturedCommand(id, request.IsFeatured), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetAdminCategories(ISender sender, CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryDto> categories = await sender.Send(new GetCategoriesQuery(), cancellationToken);
        return Results.Ok(categories);
    }

    private static async Task<IResult> CreateCategory(
        CreateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        CategoryDto category = await sender.Send(
            new CreateCategoryCommand(request.Name, request.ParentId, request.DefaultMarkup, request.SortOrder),
            cancellationToken);
        return Results.Created($"/api/categories/{category.Slug}", category);
    }

    private static async Task<IResult> UpdateCategory(
        Guid id,
        UpdateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        CategoryDto category = await sender.Send(
            new UpdateCategoryCommand(id, request.Name, request.ParentId, request.DefaultMarkup, request.SortOrder),
            cancellationToken);
        return Results.Ok(category);
    }

    private static async Task<IResult> DeleteCategory(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetAdminOrders(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        PagedResult<OrderDto> result = await sender.Send(
            new GetOrdersQuery(page, pageSize, status),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAdminOrderById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Admin can see any order (no userId restriction)
        OrderDto? order = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
        return order is null ? Results.NotFound() : Results.Ok(order);
    }

    private static async Task<IResult> UpdateWhatsApp(
        UpdateWhatsAppRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // TODO: implement UpdateWhatsAppCommand
        await Task.CompletedTask;
        return Results.NoContent();
    }
}

// Request records
public sealed record ChangeStatusRequest(string Status);
public sealed record SetFeaturedRequest(bool IsFeatured);
public sealed record CreateCategoryRequest(string Name, Guid? ParentId, decimal? DefaultMarkup, int SortOrder = 0);
public sealed record UpdateCategoryRequest(string Name, Guid? ParentId, decimal? DefaultMarkup, int SortOrder);
public sealed record UpdateWhatsAppRequest(string Phone);
