namespace SelectStoreAR.Application.DTOs;

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    decimal PriceUsd,
    int Quantity,
    decimal TotalUsd);
