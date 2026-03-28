using MediatR;
using SelectStoreAR.Application.Commands.Telegram;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class TelegramEndpoints
{
    public static void MapTelegramEndpoints(this WebApplication app)
    {
        // ── Webhook del bot (llamado por Telegram automáticamente) ────────────
        // Seguridad: valida X-Telegram-Bot-Api-Secret-Token internamente
        app.MapPost("/api/telegram/webhook", HandleWebhook)
            .WithTags("Telegram")
            .AllowAnonymous();

        // ── Sync manual (pegar texto de la lista) ─────────────────────────────
        // Seguridad: requiere Bearer token o JWT admin
        app.MapPost("/api/telegram/sync-prices", SyncPricesManual)
            .WithTags("Telegram")
            .AllowAnonymous();

        // ── Preview: parsear sin guardar ─────────────────────────────────────
        // Seguridad: requiere Bearer token o JWT admin
        app.MapPost("/api/telegram/preview-prices", PreviewPrices)
            .WithTags("Telegram")
            .AllowAnonymous();
    }

    /// <summary>
    /// Valida que el request tenga un Bearer token válido (API key o JWT admin).
    /// El token se compara contra Telegram:SyncApiKey en la configuración.
    /// Si el caller ya está autenticado como admin (JWT), también se acepta.
    /// </summary>
    private static bool IsAuthorizedForSync(HttpContext httpContext, IConfiguration configuration)
    {
        // Opción 1: JWT admin válido (ya autenticado por el middleware)
        if (httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.HasClaim("role", "admin"))
        {
            return true;
        }

        // Opción 2: API key en el header Authorization: Bearer <key>
        string? authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string token = authHeader["Bearer ".Length..].Trim();
        string? expectedKey = configuration["Telegram:SyncApiKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            // Si no hay key configurada, rechazar siempre (fail-closed)
            return false;
        }

        return string.Equals(token, expectedKey, StringComparison.Ordinal);
    }

    private static async Task<IResult> HandleWebhook(
        TelegramWebhookCommand command,
        HttpContext httpContext,
        ISender sender,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        string? expectedToken = configuration["Telegram:SecretToken"];
        if (!string.IsNullOrEmpty(expectedToken))
        {
            string? received = httpContext.Request.Headers["X-Telegram-Bot-Api-Secret-Token"];
            if (received != expectedToken)
            {
                return Results.Unauthorized();
            }
        }

        TelegramMessage? message = command.Update.ChannelPost
            ?? command.Update.EditedChannelPost
            ?? command.Update.Message
            ?? command.Update.EditedMessage;

        if (message is null)
        {
            return Results.Ok(new { status = "ok", action = "ignored" });
        }

        string? text = message.Caption ?? message.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return Results.Ok(new { status = "ok", action = "ignored", reason = "empty message" });
        }

        // Lista de precios (5+ líneas con u$número) → sync automático
        if (IsPriceList(text))
        {
            SyncPriceListResult result = await sender.Send(
                new SyncPriceListCommand(text, "telegram_webhook"),
                cancellationToken);

            return Results.Ok(new
            {
                status = "ok",
                action = "price_list_synced",
                result.Created,
                result.Updated,
                result.Skipped,
                result.Errors,
            });
        }

        // Producto individual con #importar
        if (text.Contains("#importar", StringComparison.OrdinalIgnoreCase))
        {
            TelegramWebhookResult webhookResult = await sender.Send(command, cancellationToken);
            return Results.Ok(webhookResult);
        }

        return Results.Ok(new { status = "ok", action = "ignored", reason = "not a price list nor #importar" });
    }

    private static async Task<IResult> SyncPricesManual(
        SyncPricesRequest request,
        HttpContext httpContext,
        ISender sender,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedForSync(httpContext, configuration))
        {
            return Results.Unauthorized();
        }

        SyncPriceListResult result = await sender.Send(
            new SyncPriceListCommand(request.Text, "manual"),
            cancellationToken);

        return Results.Ok(result);
    }

    private static IResult PreviewPrices(
        SyncPricesRequest request,
        HttpContext httpContext,
        IConfiguration configuration)
    {
        if (!IsAuthorizedForSync(httpContext, configuration))
        {
            return Results.Unauthorized();
        }

        Application.Services.TelegramPriceListParser.PriceListResult parsed =
            Application.Services.TelegramPriceListParser.Parse(request.Text);

        return Results.Ok(new
        {
            parsed.ParsedCount,
            parsed.SkippedCount,
            parsed.DetectedBrand,
            Items = parsed.Items.Select(i => new
            {
                i.Name,
                i.Brand,
                i.Category,
                i.PriceUsd,
                i.SizeOrVariant,
                i.Inspiration,
                i.AvailabilityStatus,
            }),
        });
    }

    private static bool IsPriceList(string text)
    {
        int priceLines = text.Split('\n').Count(line =>
            System.Text.RegularExpressions.Regex.IsMatch(
                line, @"u\s*\$\s*\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        return priceLines >= 5;
    }
}

public sealed record SyncPricesRequest(string Text);
