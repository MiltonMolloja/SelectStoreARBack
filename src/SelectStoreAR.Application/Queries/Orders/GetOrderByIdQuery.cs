using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Queries.Orders;

public sealed record GetOrderByIdQuery(Guid Id, Guid? RequestingUserId = null) : IRequest<OrderDto?>;
