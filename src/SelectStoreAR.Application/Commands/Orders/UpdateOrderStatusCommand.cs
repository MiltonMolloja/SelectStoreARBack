using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Orders;

public sealed record UpdateOrderStatusCommand(
    Guid OrderId,
    string Status,
    string? DepositType = null,
    string? Notes = null) : IRequest<OrderDto?>;

public sealed class UpdateOrderStatusHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateOrderStatusCommand, OrderDto?>
{
    public async Task<OrderDto?> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        Order? order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return null;
        }

        switch (request.Status.ToLowerInvariant())
        {
            case "deposited":
                if (string.IsNullOrEmpty(request.DepositType))
                {
                    throw new DomainException("DepositType is required for 'deposited' status");
                }

                order.MarkAsDeposited(request.DepositType);
                break;

            case "orderedfromsupplier":
                order.MarkAsOrderedFromSupplier();
                break;

            case "intransit":
                order.MarkAsInTransit();
                break;

            case "readyfordelivery":
                order.MarkAsReadyForDelivery();
                break;

            case "delivered":
                order.MarkAsDelivered();
                break;

            case "cancelled":
                order.Cancel(request.Notes);
                break;

            default:
                throw new DomainException($"Unknown order status: {request.Status}");
        }

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
