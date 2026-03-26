using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Orders;

public sealed class GetOrdersHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        OrderStatus? status = null;
        if (request.Status is not null && Enum.TryParse<OrderStatus>(request.Status, true, out OrderStatus parsed))
        {
            status = parsed;
        }

        (IReadOnlyList<Order> items, int totalCount) = await orderRepository
            .GetPagedAsync(request.Page, request.PageSize, status, request.UserId, cancellationToken)
            .ConfigureAwait(false);

        List<OrderDto> dtos = items.Select(MapToDto).ToList();
        return new PagedResult<OrderDto>(dtos, totalCount, request.Page, request.PageSize);
    }

    internal static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.CustomerName,
            order.CustomerPhone.Value,
            order.TotalUsd.Amount,
            order.TotalArs.Amount,
            order.ExchangeRateUsed,
            order.Status.ToString(),
            order.DepositType,
            order.Notes,
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.ProductSlug,
                i.PriceUsd.Amount,
                i.Quantity,
                i.PriceUsd.Amount * i.Quantity)).ToList(),
            order.CreatedAt,
            order.UpdatedAt);
    }
}
