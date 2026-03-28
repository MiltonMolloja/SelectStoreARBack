using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SelectStoreAR.Application.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace SelectStoreAR.Infrastructure.Services;

/// <summary>
/// Envía notificaciones al admin por WhatsApp usando Twilio.
/// </summary>
public sealed class TwilioWhatsAppService : INotificationService
{
    private readonly ILogger<TwilioWhatsAppService> _logger;
    private readonly string? _fromNumber;
    private readonly string? _adminNumber;
    private readonly string? _adminUrl;
    private readonly bool _isConfigured;

    public TwilioWhatsAppService(IConfiguration configuration, ILogger<TwilioWhatsAppService> logger)
    {
        _logger = logger;

        string? accountSid = configuration["Twilio:AccountSid"];
        string? authToken = configuration["Twilio:AuthToken"];
        _fromNumber = configuration["Twilio:FromNumber"];
        _adminNumber = configuration["Twilio:AdminWhatsAppNumber"];
        _adminUrl = configuration["Admin:BaseUrl"] ?? "https://admin.selectstorear.com";

        _isConfigured = !string.IsNullOrEmpty(accountSid)
                     && !string.IsNullOrEmpty(authToken)
                     && !string.IsNullOrEmpty(_fromNumber)
                     && !string.IsNullOrEmpty(_adminNumber);

        if (_isConfigured)
        {
            TwilioClient.Init(accountSid!, authToken!);
            _logger.LogInformation("Twilio WhatsApp service initialized");
        }
        else
        {
            _logger.LogWarning("Twilio WhatsApp service not configured — notifications will be skipped");
        }
    }

    public async Task NotifyPendingBatchAsync(Guid batchId, int newCount, int priceChangedCount, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            _logger.LogInformation("Twilio not configured, skipping WhatsApp notification for batch {BatchId}", batchId);
            return;
        }

        string body = FormatMessage(batchId, newCount, priceChangedCount);

        try
        {
            MessageResource message = await MessageResource.CreateAsync(
                to: new PhoneNumber(_adminNumber!),
                from: new PhoneNumber(_fromNumber!),
                body: body).ConfigureAwait(false);

            _logger.LogInformation(
                "WhatsApp notification sent: SID={Sid} batch={BatchId}",
                message.Sid,
                batchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp notification for batch {BatchId}", batchId);
        }
    }

    private string FormatMessage(Guid batchId, int newCount, int priceChangedCount)
    {
        string shortBatch = batchId.ToString()[..8];
        List<string> lines =
        [
            $"*SelectStoreAR* — Cambios pendientes",
            string.Empty,
        ];

        if (newCount > 0)
        {
            lines.Add($"*NUEVOS:* {newCount} productos");
        }

        if (priceChangedCount > 0)
        {
            lines.Add($"*PRECIO CAMBIA:* {priceChangedCount} productos");
        }

        lines.Add(string.Empty);
        lines.Add($"Aprobar: {_adminUrl}/pending/batch/{batchId}");
        lines.Add($"_Batch: {shortBatch}_");

        return string.Join("\n", lines);
    }
}
