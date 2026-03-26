using FluentValidation;
using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Products;

public sealed record SearchProductsQuery(
    string Query,
    int Page = 1,
    int PageSize = 20) : IRequest<SearchProductsResult>;

public sealed record SearchProductsResult(
    IReadOnlyList<ProductDto> Items,
    string QueryUsed,
    int TotalCount,
    int Page,
    int PageSize);

public sealed class SearchProductsValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("El término de búsqueda es requerido")
            .MinimumLength(2).WithMessage("La búsqueda debe tener al menos 2 caracteres")
            .MaximumLength(100).WithMessage("La búsqueda no puede superar 100 caracteres");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("La página debe ser mayor a cero");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("El tamaño de página debe estar entre 1 y 50");
    }
}

public sealed class SearchProductsHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IExchangeRateRepository exchangeRateRepository,
    ISiteConfigRepository siteConfigRepository)
    : IRequestHandler<SearchProductsQuery, SearchProductsResult>
{
    public async Task<SearchProductsResult> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        // Reutilizamos GetPagedAsync pasando el texto como searchQuery
        (IReadOnlyList<Product> items, int totalCount) = await productRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            categoryId: null,
            status: Domain.Enums.ProductStatus.Active,
            searchQuery: request.Query,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        ExchangeRate? exchangeRate = await exchangeRateRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        SiteConfig? markupConfig = await siteConfigRepository.GetByKeyAsync("global_markup", cancellationToken).ConfigureAwait(false);
        decimal globalMarkup = decimal.TryParse(markupConfig?.Value, out decimal gm) ? gm : 25m;

        IReadOnlyList<Category> categories = await categoryRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, Category> categoryMap = categories.ToDictionary(c => c.Id);

        List<ProductDto> dtos = items.Select(p =>
        {
            categoryMap.TryGetValue(p.CategoryId, out Category? cat);
            decimal markup = p.MarkupPercentage?.Percentage ?? cat?.DefaultMarkup?.Percentage ?? globalMarkup;
            decimal finalPriceUsd = Math.Round(p.BasePriceUsd.Amount * (1 + (markup / 100)), 2);

            List<ProductImageDto> images = p.Images.OrderBy(i => i.SortOrder)
                    .Select(i => new ProductImageDto(i.Id, i.Url, i.ThumbnailUrl, i.MediumUrl, i.AltText, i.SortOrder))
                    .ToList();

            return new ProductDto(p.Id, p.Name, p.Slug.Value, p.Description, p.Brand, p.BasePriceUsd.Amount, p.MarkupPercentage?.Percentage, finalPriceUsd, p.CategoryId, cat?.Name ?? string.Empty, cat?.Slug.Value ?? string.Empty, p.Status.ToString(), p.IsFeatured, p.Specifications, images, p.CreatedAt, p.UpdatedAt);
        }).ToList();

        return new SearchProductsResult(dtos, request.Query, totalCount, request.Page, request.PageSize);
    }
}
