using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Commands.Products;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string Brand,
    decimal BasePriceUsd,
    Guid CategoryId,
    decimal? MarkupPercentage,
    Dictionary<string, string>? Specifications) : IRequest<ProductDto>;
