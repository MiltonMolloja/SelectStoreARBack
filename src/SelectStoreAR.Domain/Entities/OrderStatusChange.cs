using SelectStoreAR.Domain.Enums;

namespace SelectStoreAR.Domain.Entities;

public sealed class OrderStatusChange
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public OrderStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public DateTime ChangedAt { get; private set; }

    private OrderStatusChange()
    {
    }

    public static OrderStatusChange Create(OrderStatus status, string? notes = null)
    {
        return new OrderStatusChange
        {
            Id = Guid.NewGuid(),
            Status = status,
            Notes = notes,
            ChangedAt = DateTime.UtcNow,
        };
    }
}
