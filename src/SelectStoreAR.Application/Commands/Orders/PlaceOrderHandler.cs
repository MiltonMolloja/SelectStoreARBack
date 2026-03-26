using System.Text;
using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Application.Commands.Orders;

public sealed class PlaceOrderHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IExchangeRateRepository exchangeRateRepository,
    ISiteConfigRepository siteConfigRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        ExchangeRate? exchangeRate = await exchangeRateRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("No active exchange rate found");

        SiteConfig? globalMarkupConfig = await siteConfigRepository.GetByKeyAsync("global_markup", cancellationToken).ConfigureAwait(false);
        decimal globalMarkup = decimal.TryParse(globalMarkupConfig?.Value, out decimal gm) ? gm : 25m;

        SiteConfig? whatsappConfig = await siteConfigRepository.GetByKeyAsync("whatsapp_phone", cancellationToken).ConfigureAwait(false);
        string whatsappPhone = whatsappConfig?.Value ?? "+5493881234567";

        List<OrderItem> orderItems = [];
        foreach (PlaceOrderItemRequest itemRequest in request.Items)
        {
            Product product = await productRepository.GetByIdAsync(itemRequest.ProductId, cancellationToken).ConfigureAwait(false)
                ?? throw new DomainException($"Product '{itemRequest.ProductId}' not found");

            decimal effectiveMarkup = product.MarkupPercentage?.Percentage ?? globalMarkup;
            decimal finalPriceUsd = Math.Round(product.BasePriceUsd.Amount * (1 + (effectiveMarkup / 100)), 2);

            orderItems.Add(OrderItem.Create(
                product.Id,
                product.Name,
                product.Slug.Value,
                finalPriceUsd,
                itemRequest.Quantity));
        }

        PhoneNumber customerPhone = PhoneNumber.Create(request.CustomerPhone);

        Order order = Order.Create(
            request.CustomerName,
            customerPhone,
            orderItems,
            exchangeRate.Rate,
            request.UserId);

        orderRepository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        string whatsappUrl = GenerateWhatsAppUrl(order, whatsappPhone);

        OrderDto orderDto = MapToDto(order);
        return new PlaceOrderResult(orderDto, whatsappUrl);
    }

    private static string GenerateWhatsAppUrl(Order order, string phone)
    {
        string phoneNumber = phone.Replace("+", string.Empty, StringComparison.Ordinal);

        StringBuilder message = new();
        message.AppendLine($"Hola! Quiero hacer un pedido:");
        message.AppendLine($"Pedido: {order.OrderNumber}");
        message.AppendLine($"Cliente: {order.CustomerName}");
        message.AppendLine();
        message.AppendLine("Productos:");

        foreach (OrderItem item in order.Items)
        {
            message.AppendLine($"- {item.ProductName} x{item.Quantity} = US$ {item.PriceUsd.Amount * item.Quantity:N2}");
        }

        message.AppendLine();
        message.AppendLine($"Total: US$ {order.TotalUsd.Amount:N2}");
        message.AppendLine($"Total ARS: $ {order.TotalArs.Amount:N0}");

        string encodedMessage = Uri.EscapeDataString(message.ToString());
        return $"https://wa.me/{phoneNumber}?text={encodedMessage}";
    }

    private static OrderDto MapToDto(Order order)
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
