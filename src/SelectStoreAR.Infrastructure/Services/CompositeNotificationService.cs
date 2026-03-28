using Microsoft.Extensions.Logging;
using SelectStoreAR.Application.Interfaces;

namespace SelectStoreAR.Infrastructure.Services;

/// <summary>
/// Envía notificaciones por WhatsApp Y Email en paralelo.
/// Si alguno falla, loguea el error y continúa con el otro.
/// </summary>
public sealed class CompositeNotificationService(
    TwilioWhatsAppService whatsApp,
    EmailNotificationService email,
    ILogger<CompositeNotificationService> logger) : INotificationService
{
    public async Task NotifyPendingBatchAsync(Guid batchId, int newCount, int priceChangedCount, CancellationToken cancellationToken = default)
    {
        List<Task> tasks =
        [
            SafeNotify("WhatsApp", () => whatsApp.NotifyPendingBatchAsync(batchId, newCount, priceChangedCount, cancellationToken)),
            SafeNotify("Email", () => email.NotifyPendingBatchAsync(batchId, newCount, priceChangedCount, cancellationToken)),
        ];

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task SafeNotify(string channel, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification via {Channel} failed", channel);
        }
    }
}
