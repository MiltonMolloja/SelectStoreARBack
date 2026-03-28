using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Landing;

public sealed class GetLandingDataHandler(
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    IExchangeRateRepository exchangeRateRepository,
    ISiteConfigRepository siteConfigRepository)
    : IRequestHandler<GetLandingDataQuery, LandingDto>
{
    public async Task<LandingDto> Handle(GetLandingDataQuery request, CancellationToken cancellationToken)
    {
        // DbContext is not thread-safe — queries must run sequentially.
        IReadOnlyList<Category> categories = await categoryRepository.GetRootCategoriesAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Product> featuredProducts = await productRepository.GetFeaturedAsync(8, cancellationToken).ConfigureAwait(false);
        ExchangeRate? exchangeRate = await exchangeRateRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<SiteConfig> configs = await siteConfigRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        Dictionary<string, string> configMap = configs.ToDictionary(c => c.Key, c => c.Value);

        decimal globalMarkup = decimal.TryParse(configMap.GetValueOrDefault("global_markup"), out decimal gm) ? gm : 25m;

        List<CategoryDto> categoryDtos = categories
            .OrderBy(c => c.SortOrder)
            .Select(c => MapCategory(c, globalMarkup))
            .ToList();

        List<ProductDto> productDtos = featuredProducts
            .Select(p => MapProduct(p, categories, globalMarkup, exchangeRate?.Rate ?? 0))
            .ToList();

        ExchangeRateDto rateDto = exchangeRate is not null
            ? new ExchangeRateDto(exchangeRate.Id, exchangeRate.Rate, exchangeRate.Type, exchangeRate.IsActive, exchangeRate.IsStale(), exchangeRate.UpdatedAt)
            : new ExchangeRateDto(Guid.Empty, 0, "blue", false, true, DateTime.UtcNow);

        SiteConfigDto siteConfigDto = new(
            configMap.GetValueOrDefault("whatsapp_phone", "+5493881234567"),
            globalMarkup,
            configMap.GetValueOrDefault("site_name", "SelectStoreAR"),
            configMap.GetValueOrDefault("instagram_url", string.Empty),
            int.TryParse(configMap.GetValueOrDefault("delivery_days"), out int days) ? days : 7);

        return new LandingDto(categoryDtos, productDtos, rateDto, siteConfigDto);
    }

    private static CategoryDto MapCategory(Category category, decimal globalMarkup)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug.Value,
            category.ParentId,
            category.DefaultMarkup?.Percentage,
            category.SortOrder,
            category.ImageUrl,
            category.Products.Count(p => p.Status == Domain.Enums.ProductStatus.Active),
            category.Children
                .OrderBy(c => c.SortOrder)
                .Select(c => MapCategory(c, globalMarkup))
                .ToList());
    }

    private static ProductDto MapProduct(Product product, IReadOnlyList<Category> categories, decimal globalMarkup, decimal exchangeRate)
    {
        Category? category = categories.FirstOrDefault(c => c.Id == product.CategoryId);

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
