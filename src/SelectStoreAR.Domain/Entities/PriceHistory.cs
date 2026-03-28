using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Entities;

/// <summary>
/// Registro histórico de un precio de producto.
/// Se crea al aprobar un cambio pendiente, al enviar cotización por WhatsApp,
/// o al editar manualmente el precio.
/// </summary>
public sealed class PriceHistory
{
    private PriceHistory()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public Money PriceUsd { get; private set; } = null!;

    public PriceHistorySource Source { get; private set; }

    /// <summary>Orden asociada (solo para WhatsAppQuote).</summary>
    public Guid? OrderId { get; private set; }

    public DateTime ChangedAt { get; private set; }

    public string? ChangedBy { get; private set; }

    // Navigation
    public Product? Product { get; private set; }

    public Order? Order { get; private set; }

    public static PriceHistory Create(
        Guid productId,
        decimal priceUsd,
        PriceHistorySource source,
        string? changedBy = null,
        Guid? orderId = null)
    {
        return new PriceHistory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            PriceUsd = Money.FromUsd(priceUsd),
            Source = source,
            OrderId = orderId,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy,
        };
    }
}
