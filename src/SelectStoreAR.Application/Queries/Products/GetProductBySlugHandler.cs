using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Products;

public sealed class GetProductBySlugHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IExchangeRateRepository exchangeRateRepository,
    ISiteConfigRepository siteConfigRepository)
    : IRequestHandler<GetProductBySlugQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository.GetBySlugAsync(request.Slug, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return null;
        }

        Category? category = await categoryRepository.GetByIdAsync(product.CategoryId, cancellationToken).ConfigureAwait(false);

        ExchangeRate? exchangeRate = await exchangeRateRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        SiteConfig? globalMarkupConfig = await siteConfigRepository.GetByKeyAsync("global_markup", cancellationToken).ConfigureAwait(false);
        decimal globalMarkup = decimal.TryParse(globalMarkupConfig?.Value, out decimal gm) ? gm : 25m;

        decimal effectiveMarkup = product.MarkupPercentage?.Percentage
            ?? category?.DefaultMarkup?.Percentage
            ?? globalMarkup;

        decimal finalPriceUsd = Math.Round(product.BasePriceUsd.Amount * (1 + (effectiveMarkup / 100)), 2);

        return new ProductDto(
            product.Id,
            product.Name,
            product.Slug.Value,
            product.Description,
            product.Brand,
            product.BasePriceUsd.Amount,
            product.MarkupPercentage?.Percentage,
            finalPriceUsd,
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
