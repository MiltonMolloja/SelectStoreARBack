namespace SelectStoreAR.Application.DTOs;

public sealed record DashboardDto(
    ProductStatsDto Products,
    OrderStatsDto Orders,
    ExchangeRateDto? ExchangeRate,
    IReadOnlyList<TopProductDto> TopProducts);

public sealed record ProductStatsDto(
    int Total,
    int Active,
    int Draft,
    int Inactive);

public sealed record OrderStatsDto(
    int Today,
    int ThisWeek,
    int ThisMonth,
    IReadOnlyDictionary<string, int> ByStatus,
    double AverageDeliveryDays,
    int Delayed);

public sealed record TopProductDto(
    string Name,
    string Slug,
    int OrderCount);
