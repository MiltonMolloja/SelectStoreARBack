namespace SelectStoreAR.Domain.Enums;

/// <summary>
/// Tipo de cambio pendiente de aprobación importado desde Telegram.
/// </summary>
public enum PendingChangeType
{
    /// <summary>Producto nuevo que no existía en la DB.</summary>
    Created = 0,

    /// <summary>El precio USD cambió respecto al valor actual.</summary>
    PriceChanged = 1,

    /// <summary>La disponibilidad cambió (stock, depósito, etc.).</summary>
    AvailabilityChanged = 2,
}
