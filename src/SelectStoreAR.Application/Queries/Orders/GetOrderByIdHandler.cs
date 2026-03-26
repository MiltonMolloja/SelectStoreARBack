using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Orders;

public sealed class GetOrderByIdHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        Order? order = await orderRepository
            .GetByIdAsync(request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            return null;
        }

        // Si hay un usuario solicitante que no es admin, verificar que el pedido le pertenece
        if (request.RequestingUserId.HasValue && order.UserId != request.RequestingUserId.Value)
        {
            throw new DomainException("You don't have permission to view this order");
        }

        return GetOrdersHandler.MapToDto(order);
    }
}
