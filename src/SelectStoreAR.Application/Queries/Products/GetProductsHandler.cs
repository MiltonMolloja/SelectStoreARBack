using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Products;

public sealed class GetProductsHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IExchangeRateRepository exchangeRateRepository,
    ISiteConfigRepository siteConfigRepository)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        ProductStatus? status = null;
        if (request.Status is not null && Enum.TryParse<ProductStatus>(request.Status, true, out ProductStatus parsedStatus))
        {
            status = parsedStatus;
        }

        (IReadOnlyList<Product> items, int totalCount) = await productRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.CategoryId,
            status ?? ProductStatus.Active,
            request.SearchQuery,
            request.MinPriceUsd,
            request.MaxPriceUsd,
            request.Brand,
            cancellationToken).ConfigureAwait(false);

        ExchangeRate? exchangeRate = await exchangeRateRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        SiteConfig? globalMarkupConfig = await siteConfigRepository.GetByKeyAsync("global_markup", cancellationToken).ConfigureAwait(false);
        decimal globalMarkup = decimal.TryParse(globalMarkupConfig?.Value, out decimal gm) ? gm : 25m;

        IReadOnlyList<Category> categories = await categoryRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, Category> categoryMap = categories.ToDictionary(c => c.Id);

        List<ProductDto> dtos = items.Select(p => MapToDto(p, categoryMap, globalMarkup, exchangeRate?.Rate ?? 0)).ToList();

        return new PagedResult<ProductDto>(dtos, totalCount, request.Page, request.PageSize);
    }

    private static ProductDto MapToDto(Product product, Dictionary<Guid, Category> categoryMap, decimal globalMarkup, decimal exchangeRate)
    {
        categoryMap.TryGetValue(product.CategoryId, out Category? category);

        decimal effectiveMarkup = product.MarkupPercentage?.Percentage
            ?? category?.DefaultMarkup?.Percentage
            ?? globalMarkup;

        decimal finalPriceUsd = product.BasePriceUsd.Amount * (1 + (effectiveMarkup / 100));

        return new ProductDto(
            product.Id,
            product.Name,
            product.Slug.Value,
            product.Description,
            product.Brand,
            product.BasePriceUsd.Amount,
            product.MarkupPercentage?.Percentage,
            Math.Round(finalPriceUsd, 2),
            product.CategoryId,
            category?.Name ?? string.Empty,
            category?.Slug.Value ?? string.Empty,
            product.Status.ToString(),
            product.IsFeatured,
            product.Specifications,
            product.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageDto(i.Id, i.Url, i.ThumbnailUrl, i.MediumUrl, i.AltText, i.SortOrder))
                .ToList(),
            product.CreatedAt,
            product.UpdatedAt);
    }
}
