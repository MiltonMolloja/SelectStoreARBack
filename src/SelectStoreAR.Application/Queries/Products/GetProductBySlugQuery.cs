using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Queries.Products;

public sealed record GetProductBySlugQuery(string Slug) : IRequest<ProductDto?>;
