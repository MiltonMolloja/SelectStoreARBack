using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Events;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Entities;

/// <summary>
/// Cambio pendiente de aprobación importado desde Telegram.
/// El admin debe aprobar o rechazar antes de que se aplique al producto real.
/// Solo puede existir un cambio Pending por producto a la vez.
/// </summary>
public sealed class ProductPendingChange : BaseEntity
{
    private ProductPendingChange()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>NULL si es un producto nuevo que aún no existe en la DB.</summary>
    public Guid? ProductId { get; private set; }

    /// <summary>Agrupa todos los cambios del mismo mensaje de Telegram.</summary>
    public Guid TelegramSyncBatchId { get; private set; }

    public string? TelegramMessageId { get; private set; }

    public PendingChangeType ChangeType { get; private set; }

    public PendingChangeStatus Status { get; private set; }

    /// <summary>Línea exacta del mensaje de Telegram (para auditoría).</summary>
    public string RawTelegramText { get; private set; } = string.Empty;

    public string ProposedName { get; private set; } = string.Empty;

    public string ProposedBrand { get; private set; } = string.Empty;

    public string ProposedDescription { get; private set; } = string.Empty;

    public Money ProposedPriceUsd { get; private set; } = null!;

    public AvailabilityStatus ProposedAvailability { get; private set; }

    public string? ProposedInspiration { get; private set; }

    public string ProposedCategory { get; private set; } = string.Empty;

    /// <summary>Precio anterior del producto (null si es nuevo).</summary>
    public Money? CurrentPriceUsd { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    public string? ReviewedBy { get; private set; }

    public string? ReviewNote { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Product? Product { get; private set; }

    /// <summary>
    /// Crea un cambio pendiente para un producto nuevo.
    /// </summary>
    public static ProductPendingChange CreateNew(
        Guid batchId,
        string? telegramMessageId,
        string rawTelegramText,
        string proposedName,
        string proposedBrand,
        decimal proposedPriceUsd,
        AvailabilityStatus proposedAvailability,
        string? proposedInspiration,
        string proposedCategory)
    {
        return new ProductPendingChange
        {
            Id = Guid.NewGuid(),
            ProductId = null,
            TelegramSyncBatchId = batchId,
            TelegramMessageId = telegramMessageId,
            ChangeType = PendingChangeType.Created,
            Status = PendingChangeStatus.Pending,
            RawTelegramText = rawTelegramText,
            ProposedName = proposedName,
            ProposedBrand = proposedBrand,
            ProposedDescription = string.Empty,
            ProposedPriceUsd = Money.FromUsd(proposedPriceUsd),
            ProposedAvailability = proposedAvailability,
            ProposedInspiration = proposedInspiration,
            ProposedCategory = proposedCategory,
            CurrentPriceUsd = null,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Crea un cambio pendiente para un producto existente (precio o disponibilidad).
    /// </summary>
    public static ProductPendingChange CreateUpdate(
        Guid productId,
        Guid batchId,
        string? telegramMessageId,
        string rawTelegramText,
        string proposedName,
        string proposedBrand,
        decimal proposedPriceUsd,
        AvailabilityStatus proposedAvailability,
        string? proposedInspiration,
        string proposedCategory,
        decimal currentPriceUsd,
        PendingChangeType changeType)
    {
        return new ProductPendingChange
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            TelegramSyncBatchId = batchId,
            TelegramMessageId = telegramMessageId,
            ChangeType = changeType,
            Status = PendingChangeStatus.Pending,
            RawTelegramText = rawTelegramText,
            ProposedName = proposedName,
            ProposedBrand = proposedBrand,
            ProposedDescription = string.Empty,
            ProposedPriceUsd = Money.FromUsd(proposedPriceUsd),
            ProposedAvailability = proposedAvailability,
            ProposedInspiration = proposedInspiration,
            ProposedCategory = proposedCategory,
            CurrentPriceUsd = Money.FromUsd(currentPriceUsd),
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Reemplaza los datos propuestos con valores más recientes (mismo producto, nuevo sync).
    /// </summary>
    public void ReplaceWith(
        Guid newBatchId,
        string? telegramMessageId,
        string rawTelegramText,
        decimal proposedPriceUsd,
        AvailabilityStatus proposedAvailability,
        string? proposedInspiration,
        PendingChangeType changeType)
    {
        if (Status != PendingChangeStatus.Pending)
        {
            throw new DomainException("Cannot replace a change that is not pending");
        }

        TelegramSyncBatchId = newBatchId;
        TelegramMessageId = telegramMessageId;
        RawTelegramText = rawTelegramText;
        ProposedPriceUsd = Money.FromUsd(proposedPriceUsd);
        ProposedAvailability = proposedAvailability;
        ProposedInspiration = proposedInspiration;
        ChangeType = changeType;
    }

    public void Approve(string reviewedBy)
    {
        if (Status != PendingChangeStatus.Pending)
        {
            throw new DomainException("Only pending changes can be approved");
        }

        Status = PendingChangeStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;

        AddDomainEvent(new PendingChangeApprovedEvent(Id, ProductId, ChangeType));
    }

    public void Reject(string reviewedBy, string? note = null)
    {
        if (Status != PendingChangeStatus.Pending)
        {
            throw new DomainException("Only pending changes can be rejected");
        }

        Status = PendingChangeStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;
        ReviewNote = note;

        AddDomainEvent(new PendingChangeRejectedEvent(Id, ProductId, note));
    }
}
