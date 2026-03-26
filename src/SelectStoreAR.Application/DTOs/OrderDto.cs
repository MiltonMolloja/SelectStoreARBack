namespace SelectStoreAR.Application.DTOs;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid? UserId,
    string CustomerName,
    string CustomerPhone,
    decimal TotalUsd,
    decimal TotalArs,
    decimal ExchangeRateUsed,
    string Status,
    string? DepositType,
    string? Notes,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt);
