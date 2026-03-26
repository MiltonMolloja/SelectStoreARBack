using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Categories;

public sealed class GetCategoriesHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Category> roots = await categoryRepository
            .GetRootCategoriesAsync(cancellationToken)
            .ConfigureAwait(false);

        return roots.OrderBy(c => c.SortOrder).Select(MapCategory).ToList();
    }

    private static CategoryDto MapCategory(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug.Value,
            category.ParentId,
            category.DefaultMarkup?.Percentage,
            category.SortOrder,
            category.ImageUrl,
            category.Products.Count(p => p.Status == ProductStatus.Active),
            category.Children
                .OrderBy(c => c.SortOrder)
                .Select(MapCategory)
                .ToList());
    }
}
