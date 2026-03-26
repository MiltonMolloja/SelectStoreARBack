using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Entities;

public sealed class OrderItem
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string ProductSlug { get; private set; } = string.Empty;

    public Money PriceUsd { get; private set; } = null!;

    public int Quantity { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private OrderItem()
    {
    }

    public static OrderItem Create(
        Guid productId,
        string productName,
        string productSlug,
        decimal priceUsd,
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero");
        }

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            ProductSlug = productSlug,
            PriceUsd = Money.FromUsd(priceUsd),
            Quantity = quantity,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
