using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Queries.Products;

public sealed record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    string? Status = null,
    string? SearchQuery = null,
    decimal? MinPriceUsd = null,
    decimal? MaxPriceUsd = null,
    string? Brand = null) : IRequest<PagedResult<ProductDto>>;
