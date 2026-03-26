using FluentAssertions;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Events;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Tests.Entities;

public sealed class OrderTests
{
    private static Order CreateSampleOrder()
    {
        OrderItem item = OrderItem.Create(Guid.NewGuid(), "Samsung Galaxy S26 Ultra", "samsung-galaxy-s26-ultra", 1250m, 1);
        PhoneNumber phone = PhoneNumber.Create("+5493881234567");
        return Order.Create("Juan Perez", phone, [item], 1250m);
    }

    [Fact]
    public void Create_WithValidData_CreatesOrderWithSentStatus()
    {
        Order order = CreateSampleOrder();

        order.Status.Should().Be(OrderStatus.Sent);
        order.OrderNumber.Should().StartWith("SSA-");
        order.Items.Should().HaveCount(1);
        order.TotalUsd.Amount.Should().Be(1250m);
    }

    [Fact]
    public void Create_WithNoItems_ThrowsDomainException()
    {
        PhoneNumber phone = PhoneNumber.Create("+5493881234567");
        Action act = () => Order.Create("Juan", phone, [], 1250m);

        act.Should().Throw<DomainException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public void Create_RaisesOrderPlacedEvent()
    {
        Order order = CreateSampleOrder();

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlacedEvent>();
    }

    [Fact]
    public void MarkAsDeposited_FromSent_ChangesStatusToDeposited()
    {
        Order order = CreateSampleOrder();
        order.MarkAsDeposited("transferencia");

        order.Status.Should().Be(OrderStatus.Deposited);
        order.DepositType.Should().Be("transferencia");
    }

    [Fact]
    public void MarkAsDeposited_WithInvalidDepositType_ThrowsDomainException()
    {
        Order order = CreateSampleOrder();

        Action act = () => order.MarkAsDeposited("efectivo");

        act.Should().Throw<DomainException>()
            .WithMessage("*persona*transferencia*");
    }

    [Fact]
    public void MarkAsDeposited_WithPersona_IsValid()
    {
        Order order = CreateSampleOrder();
        order.MarkAsDeposited("persona");

        order.DepositType.Should().Be("persona");
    }

    [Fact]
    public void Cancel_FromDelivered_ThrowsDomainException()
    {
        Order order = CreateSampleOrder();
        order.MarkAsDeposited("transferencia");
        order.MarkAsOrderedFromSupplier();
        order.MarkAsInTransit();
        order.MarkAsReadyForDelivery();
        order.MarkAsDelivered();

        Action act = () => order.Cancel();

        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot cancel a delivered*");
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsDomainException()
    {
        Order order = CreateSampleOrder();
        order.Cancel();

        Action act = () => order.Cancel();

        act.Should().Throw<DomainException>()
            .WithMessage("*already cancelled*");
    }

    [Fact]
    public void CompleteFlow_AllTransitions_Succeed()
    {
        Order order = CreateSampleOrder();

        order.MarkAsDeposited("transferencia");
        order.Status.Should().Be(OrderStatus.Deposited);

        order.MarkAsOrderedFromSupplier();
        order.Status.Should().Be(OrderStatus.OrderedFromSupplier);

        order.MarkAsInTransit();
        order.Status.Should().Be(OrderStatus.InTransit);

        order.MarkAsReadyForDelivery();
        order.Status.Should().Be(OrderStatus.ReadyForDelivery);

        order.MarkAsDelivered();
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void InvalidTransition_FromSentToDelivered_ThrowsDomainException()
    {
        Order order = CreateSampleOrder();

        Action act = () => order.MarkAsDelivered();

        act.Should().Throw<DomainException>()
            .WithMessage("*transition*");
    }

    [Fact]
    public void TotalArs_IsCalculatedFromUsdAndExchangeRate()
    {
        OrderItem item = OrderItem.Create(Guid.NewGuid(), "Test Product", "test-product", 100m, 2);
        PhoneNumber phone = PhoneNumber.Create("+5493881234567");
        Order order = Order.Create("Test Customer", phone, [item], 1250m);

        order.TotalUsd.Amount.Should().Be(200m);
        order.TotalArs.Amount.Should().Be(250000m);
    }
}
