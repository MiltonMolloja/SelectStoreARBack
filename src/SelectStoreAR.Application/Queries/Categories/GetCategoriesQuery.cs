using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Queries.Categories;

public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;
