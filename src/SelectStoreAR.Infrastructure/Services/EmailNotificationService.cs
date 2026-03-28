using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SelectStoreAR.Application.Interfaces;

namespace SelectStoreAR.Infrastructure.Services;

/// <summary>
/// Envía notificaciones al admin por email usando SMTP (MailKit).
/// </summary>
public sealed class EmailNotificationService : INotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUser;
    private readonly string? _smtpPassword;
    private readonly string? _fromAddress;
    private readonly string? _adminAddress;
    private readonly string? _adminUrl;
    private readonly bool _isConfigured;

    public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
    {
        _logger = logger;

        _smtpHost = configuration["Email:SmtpHost"];
        _smtpPort = int.TryParse(configuration["Email:SmtpPort"], out int port) ? port : 587;
        _smtpUser = configuration["Email:SmtpUser"];
        _smtpPassword = configuration["Email:SmtpPassword"];
        _fromAddress = configuration["Email:FromAddress"];
        _adminAddress = configuration["Email:AdminAddress"];
        _adminUrl = configuration["Admin:BaseUrl"] ?? "https://admin.selectstorear.com";

        _isConfigured = !string.IsNullOrEmpty(_smtpHost)
                     && !string.IsNullOrEmpty(_fromAddress)
                     && !string.IsNullOrEmpty(_adminAddress);

        if (!_isConfigured)
        {
            _logger.LogWarning("Email notification service not configured — notifications will be skipped");
        }
    }

    public async Task NotifyPendingBatchAsync(Guid batchId, int newCount, int priceChangedCount, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            _logger.LogInformation("Email not configured, skipping notification for batch {BatchId}", batchId);
            return;
        }

        MimeMessage message = new();
        message.From.Add(new MailboxAddress("SelectStoreAR", _fromAddress));
        message.To.Add(new MailboxAddress("Admin", _adminAddress));
        message.Subject = $"SelectStoreAR — {newCount + priceChangedCount} cambios pendientes de aprobacion";

        string approveUrl = $"{_adminUrl}/pending/batch/{batchId}";

        message.Body = new TextPart("html")
        {
            Text = $"""
                <h2>Cambios pendientes de aprobacion</h2>
                <ul>
                    {(newCount > 0 ? $"<li><strong>Nuevos:</strong> {newCount} productos</li>" : string.Empty)}
                    {(priceChangedCount > 0 ? $"<li><strong>Precio cambia:</strong> {priceChangedCount} productos</li>" : string.Empty)}
                </ul>
                <p><a href="{approveUrl}">Aprobar todo</a></p>
                <p><small>Batch: {batchId}</small></p>
                """,
        };

        try
        {
            using SmtpClient client = new();
            await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(_smtpUser))
            {
                await client.AuthenticateAsync(_smtpUser, _smtpPassword, cancellationToken).ConfigureAwait(false);
            }

            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Email notification sent for batch {BatchId} to {Admin}", batchId, _adminAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification for batch {BatchId}", batchId);
        }
    }
}
