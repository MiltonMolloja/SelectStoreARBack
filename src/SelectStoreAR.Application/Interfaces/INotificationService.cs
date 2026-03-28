namespace SelectStoreAR.Application.Interfaces;

/// <summary>
/// Servicio de notificaciones al admin (WhatsApp + Email).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Notifica al admin que hay un batch de cambios pendientes de aprobación.
    /// </summary>
    Task NotifyPendingBatchAsync(Guid batchId, int newCount, int priceChangedCount, CancellationToken cancellationToken = default);
}
