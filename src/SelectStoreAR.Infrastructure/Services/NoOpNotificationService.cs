using Microsoft.Extensions.Logging;
using SelectStoreAR.Application.Interfaces;

namespace SelectStoreAR.Infrastructure.Services;

/// <summary>
/// Stub de notificaciones que solo loguea. Se reemplaza por Twilio/Email en la Fase 7.
/// </summary>
public sealed class NoOpNotificationService(ILogger<NoOpNotificationService> logger) : INotificationService
{
    public Task NotifyPendingBatchAsync(Guid batchId, int newCount, int priceChangedCount, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Notification stub: batch {BatchId} has {New} new + {Changed} price changes pending approval",
            batchId,
            newCount,
            priceChangedCount);

        return Task.CompletedTask;
    }
}
