namespace SelectStoreAR.Domain.Enums;

/// <summary>
/// Origen del registro en el historial de precios.
/// </summary>
public enum PriceHistorySource
{
    /// <summary>Precio detectado en un sync de Telegram.</summary>
    TelegramSync = 0,

    /// <summary>Precio registrado al enviar cotización por WhatsApp.</summary>
    WhatsAppQuote = 1,

    /// <summary>Precio aplicado al aprobar un cambio pendiente.</summary>
    Approved = 2,

    /// <summary>Precio editado manualmente por el admin.</summary>
    Manual = 3,
}
