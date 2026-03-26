using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Commands.Orders;

public sealed record PlaceOrderCommand(
    string CustomerName,
    string CustomerPhone,
    IReadOnlyList<PlaceOrderItemRequest> Items,
    Guid? UserId = null) : IRequest<PlaceOrderResult>;

public sealed record PlaceOrderItemRequest(
    Guid ProductId,
    int Quantity);

public sealed record PlaceOrderResult(
    OrderDto Order,
    string WhatsAppUrl);
