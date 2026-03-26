using MediatR;
using SelectStoreAR.Application.Commands.Telegram;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class TelegramEndpoints
{
    public static void MapTelegramEndpoints(this WebApplication app)
    {
        app.MapPost("/api/telegram/webhook", HandleWebhook)
            .WithTags("Telegram")
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleWebhook(
        TelegramWebhookCommand command,
        HttpContext httpContext,
        ISender sender,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        // Validate secret token
        string? expectedToken = configuration["Telegram:SecretToken"];
        if (!string.IsNullOrEmpty(expectedToken))
        {
            string? receivedToken = httpContext.Request.Headers["X-Telegram-Bot-Api-Secret-Token"];
            if (receivedToken != expectedToken)
            {
                return Results.Unauthorized();
            }
        }

        TelegramWebhookResult result = await sender.Send(command, cancellationToken);
        return Results.Ok(result);
    }
}
