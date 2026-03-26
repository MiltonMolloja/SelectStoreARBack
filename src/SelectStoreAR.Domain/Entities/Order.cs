using System.Globalization;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Events;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Entities;

public sealed class Order : BaseEntity
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusChange> _statusHistory = [];

    private static readonly Dictionary<OrderStatus, OrderStatus[]> _validTransitions = new()
    {
        [OrderStatus.Sent] = [OrderStatus.Deposited, OrderStatus.Cancelled],
        [OrderStatus.Deposited] = [OrderStatus.OrderedFromSupplier, OrderStatus.Cancelled],
        [OrderStatus.OrderedFromSupplier] = [OrderStatus.InTransit, OrderStatus.Cancelled],
        [OrderStatus.InTransit] = [OrderStatus.ReadyForDelivery, OrderStatus.Cancelled],
        [OrderStatus.ReadyForDelivery] = [OrderStatus.Delivered, OrderStatus.Cancelled],
    };

    private Order()
    {
    }

    public Guid Id { get; private set; }

    public string OrderNumber { get; private set; } = string.Empty;

    public Guid? UserId { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;

    public PhoneNumber CustomerPhone { get; private set; } = null!;

    public Money TotalUsd { get; private set; } = null!;

    public Money TotalArs { get; private set; } = null!;

    public decimal ExchangeRateUsed { get; private set; }

    public OrderStatus Status { get; private set; }

    public string? DepositType { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public IReadOnlyList<OrderStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    public static Order Create(
        string customerName,
        PhoneNumber customerPhone,
        IReadOnlyList<OrderItem> items,
        decimal exchangeRate,
        Guid? userId = null)
    {
        if (items.Count == 0)
        {
            throw new DomainException("Order must have at least one item");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            ExchangeRateUsed = exchangeRate,
            Status = OrderStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        foreach (OrderItem item in items)
        {
            order._items.Add(item);
        }

        order.TotalUsd = Money.FromUsd(items.Sum(i => i.PriceUsd.Amount * i.Quantity));
        order.TotalArs = Money.FromArs(Math.Round(items.Sum(i => i.PriceUsd.Amount * i.Quantity) * exchangeRate, 0));

        order._statusHistory.Add(OrderStatusChange.Create(OrderStatus.Sent));
        order.AddDomainEvent(new OrderPlacedEvent(order.Id, order.OrderNumber));

        return order;
    }

    public void MarkAsDeposited(string depositType)
    {
        ValidateTransition(OrderStatus.Deposited);

        if (depositType is not ("persona" or "transferencia"))
        {
            throw new DomainException("Deposit type must be 'persona' or 'transferencia'");
        }

        Status = OrderStatus.Deposited;
        DepositType = depositType;
        UpdatedAt = DateTime.UtcNow;
        _statusHistory.Add(OrderStatusChange.Create(OrderStatus.Deposited));
    }

    public void MarkAsOrderedFromSupplier()
    {
        ValidateTransition(OrderStatus.OrderedFromSupplier);
        Status = OrderStatus.OrderedFromSupplier;
        UpdatedAt = DateTime.UtcNow;
        _statusHistory.Add(OrderStatusChange.Create(OrderStatus.OrderedFromSupplier));
    }

    public void MarkAsInTransit()
    {
        ValidateTransition(OrderStatus.InTransit);
        Status = OrderStatus.InTransit;
        UpdatedAt = DateTime.UtcNow;
        _statusHistory.Add(OrderStatusChange.Create(OrderStatus.InTransit));
    }

    public void MarkAsReadyForDelivery()
    {
        ValidateTransition(OrderStatus.ReadyForDelivery);
        Status = OrderStatus.ReadyForDelivery;
        UpdatedAt = DateTime.UtcNow;
        _statusHistory.Add(OrderStatusChange.Create(OrderStatus.ReadyForDelivery));
    }

    public void MarkAsDelivered()
    {
        ValidateTransition(OrderStatus.Delivered);
        Status = OrderStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
        _statusHistory.Add(OrderStatusChange.Create(OrderStatus.Delivered));
    }

    public void Cancel(string? reason = null)
    {
        if (Status == OrderStatus.Delivered)
        {
            throw new DomainException("Cannot cancel a delivered order");
        }

        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException("Order is already cancelled");
        }

        Status = OrderStatus.Cancelled;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
        _statusHistory.Add(OrderStatusChange.Create(OrderStatus.Cancelled, reason));
    }

    private void ValidateTransition(OrderStatus newStatus)
    {
        if (!_validTransitions.TryGetValue(Status, out OrderStatus[]? validNext) || !validNext.Contains(newStatus))
        {
            throw new DomainException($"Cannot transition from '{Status}' to '{newStatus}'");
        }
    }

    private static string GenerateOrderNumber()
    {
        string date = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string random = Random.Shared.Next(1, 999).ToString("D3", CultureInfo.InvariantCulture);
        return $"SSA-{date}-{random}";
    }
}
