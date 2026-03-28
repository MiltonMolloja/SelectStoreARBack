using FluentAssertions;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Events;

namespace SelectStoreAR.Domain.Tests.Entities;

public sealed class ProductPendingChangeTests
{
    private static ProductPendingChange CreateNewPending(decimal price = 100m)
    {
        return ProductPendingChange.CreateNew(
            batchId: Guid.NewGuid(),
            telegramMessageId: "msg#1283",
            rawTelegramText: "Test Product u$100",
            proposedName: "Test Product",
            proposedBrand: "TestBrand",
            proposedPriceUsd: price,
            proposedAvailability: AvailabilityStatus.Available,
            proposedInspiration: null,
            proposedCategory: "Tecnologia");
    }

    private static ProductPendingChange CreateUpdatePending(decimal newPrice = 200m, decimal oldPrice = 100m)
    {
        return ProductPendingChange.CreateUpdate(
            productId: Guid.NewGuid(),
            batchId: Guid.NewGuid(),
            telegramMessageId: "msg#1285",
            rawTelegramText: "Test Product u$200",
            proposedName: "Test Product",
            proposedBrand: "TestBrand",
            proposedPriceUsd: newPrice,
            proposedAvailability: AvailabilityStatus.Warehouse,
            proposedInspiration: "Dior-Sauvage",
            proposedCategory: "Perfumes",
            currentPriceUsd: oldPrice,
            changeType: PendingChangeType.PriceChanged);
    }

    [Fact]
    public void CreateNew_SetsCorrectDefaults()
    {
        ProductPendingChange change = CreateNewPending();

        change.Id.Should().NotBeEmpty();
        change.ProductId.Should().BeNull();
        change.ChangeType.Should().Be(PendingChangeType.Created);
        change.Status.Should().Be(PendingChangeStatus.Pending);
        change.ProposedName.Should().Be("Test Product");
        change.ProposedBrand.Should().Be("TestBrand");
        change.ProposedPriceUsd.Amount.Should().Be(100m);
        change.CurrentPriceUsd.Should().BeNull();
        change.ReviewedAt.Should().BeNull();
        change.ReviewedBy.Should().BeNull();
    }

    [Fact]
    public void CreateUpdate_SetsProductIdAndCurrentPrice()
    {
        ProductPendingChange change = CreateUpdatePending(200m, 100m);

        change.ProductId.Should().NotBeNull();
        change.ChangeType.Should().Be(PendingChangeType.PriceChanged);
        change.ProposedPriceUsd.Amount.Should().Be(200m);
        change.CurrentPriceUsd!.Amount.Should().Be(100m);
    }

    [Fact]
    public void Approve_SetStatusAndReviewedFields()
    {
        ProductPendingChange change = CreateNewPending();

        change.Approve("admin@test.com");

        change.Status.Should().Be(PendingChangeStatus.Approved);
        change.ReviewedBy.Should().Be("admin@test.com");
        change.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_RaisesDomainEvent()
    {
        ProductPendingChange change = CreateNewPending();

        change.Approve("admin");

        change.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PendingChangeApprovedEvent>()
            .Which.ChangeType.Should().Be(PendingChangeType.Created);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsDomainException()
    {
        ProductPendingChange change = CreateNewPending();
        change.Approve("admin");
        change.ClearDomainEvents();

        Action act = () => change.Approve("admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*pending*");
    }

    [Fact]
    public void Reject_SetsStatusAndNote()
    {
        ProductPendingChange change = CreateUpdatePending();

        change.Reject("admin@test.com", "Price too high");

        change.Status.Should().Be(PendingChangeStatus.Rejected);
        change.ReviewedBy.Should().Be("admin@test.com");
        change.ReviewNote.Should().Be("Price too high");
        change.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_RaisesDomainEvent()
    {
        ProductPendingChange change = CreateUpdatePending();

        change.Reject("admin", "Wrong price");

        change.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PendingChangeRejectedEvent>()
            .Which.Note.Should().Be("Wrong price");
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ThrowsDomainException()
    {
        ProductPendingChange change = CreateNewPending();
        change.Reject("admin");
        change.ClearDomainEvents();

        Action act = () => change.Reject("admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*pending*");
    }

    [Fact]
    public void ReplaceWith_UpdatesProposedFields()
    {
        ProductPendingChange change = CreateUpdatePending(200m, 100m);
        Guid newBatchId = Guid.NewGuid();

        change.ReplaceWith(
            newBatchId,
            telegramMessageId: "msg#1290",
            rawTelegramText: "Test Product u$250",
            proposedPriceUsd: 250m,
            proposedAvailability: AvailabilityStatus.Available,
            proposedInspiration: "New-Inspiration",
            changeType: PendingChangeType.PriceChanged);

        change.TelegramSyncBatchId.Should().Be(newBatchId);
        change.ProposedPriceUsd.Amount.Should().Be(250m);
        change.ProposedAvailability.Should().Be(AvailabilityStatus.Available);
        change.ProposedInspiration.Should().Be("New-Inspiration");
    }

    [Fact]
    public void ReplaceWith_WhenNotPending_ThrowsDomainException()
    {
        ProductPendingChange change = CreateNewPending();
        change.Approve("admin");
        change.ClearDomainEvents();

        Action act = () => change.ReplaceWith(
            Guid.NewGuid(), null, "text", 100m,
            AvailabilityStatus.Available, null,
            PendingChangeType.Created);

        act.Should().Throw<DomainException>()
            .WithMessage("*not pending*");
    }
}
