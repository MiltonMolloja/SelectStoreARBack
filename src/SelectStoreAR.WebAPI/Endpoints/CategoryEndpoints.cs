using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Queries.Categories;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/", GetCategories)
            .WithName("GetCategories")
            .Produces<IReadOnlyList<CategoryDto>>()
            .CacheOutput("categories");

        group.MapGet("/{slug}", GetCategoryBySlug)
            .WithName("GetCategoryBySlug")
            .Produces<CategoryDto>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetCategories(
        ISender sender,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryDto> categories = await sender.Send(new GetCategoriesQuery(), cancellationToken);
        return Results.Ok(categories);
    }

    private static async Task<IResult> GetCategoryBySlug(
        string slug,
        ISender sender,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryDto> all = await sender.Send(new GetCategoriesQuery(), cancellationToken);
        CategoryDto? found = FindBySlug(all, slug);
        return found is null ? Results.NotFound() : Results.Ok(found);
    }

    private static CategoryDto? FindBySlug(IReadOnlyList<CategoryDto> categories, string slug)
    {
        foreach (CategoryDto cat in categories)
        {
            if (cat.Slug == slug)
            {
                return cat;
            }

            CategoryDto? found = FindBySlug(cat.Children, slug);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
