using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Queries.Orders;

public sealed record GetOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    Guid? UserId = null) : IRequest<PagedResult<OrderDto>>;
