namespace SelectStoreAR.Application.DTOs;

public sealed record ExchangeRateDto(
    Guid Id,
    decimal Rate,
    string Type,
    bool IsActive,
    bool IsStale,
    DateTime UpdatedAt);
