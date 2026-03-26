using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Commands.Products;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    string Brand,
    decimal BasePriceUsd,
    Guid CategoryId,
    decimal? MarkupPercentage,
    bool IsFeatured,
    Dictionary<string, string>? Specifications) : IRequest<ProductDto>;
