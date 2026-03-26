namespace SelectStoreAR.Application.DTOs;

public sealed record LandingDto(
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<ProductDto> FeaturedProducts,
    ExchangeRateDto ExchangeRate,
    SiteConfigDto SiteConfig);
