namespace SelectStoreAR.Application.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string Brand,
    decimal BasePriceUsd,
    decimal? MarkupPercentage,
    decimal FinalPriceUsd,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    string Status,
    bool IsFeatured,
    Dictionary<string, string> Specifications,
    IReadOnlyList<ProductImageDto> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt);
