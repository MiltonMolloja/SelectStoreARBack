using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SelectStoreAR.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace SelectStoreAR.Infrastructure.Services;

public sealed class TelegramImageService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ITelegramImageService
{
    private static readonly WebpEncoder WebpEncoder = new() { Quality = 85 };

    public async Task<string> DownloadAndSaveAsync(
        string fileId,
        Guid productId,
        int imageIndex,
        CancellationToken cancellationToken = default)
    {
        string botToken = configuration["Telegram:BotToken"]
            ?? throw new InvalidOperationException("Telegram:BotToken not configured");

        string basePath = configuration["Images:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
        string baseUrl = configuration["Images:BaseUrl"] ?? "/images/products";

        HttpClient httpClient = httpClientFactory.CreateClient("Telegram");

        // Paso 1: obtener file path de Telegram
        Uri getFileUri = new($"https://api.telegram.org/bot{botToken}/getFile?file_id={fileId}");
        using HttpResponseMessage fileResponse = await httpClient.GetAsync(getFileUri, cancellationToken).ConfigureAwait(false);
        fileResponse.EnsureSuccessStatusCode();

        string fileJson = await fileResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        JsonDocument doc = JsonDocument.Parse(fileJson);
        string? filePath = doc.RootElement
            .GetProperty("result")
            .GetProperty("file_path")
            .GetString();

        if (string.IsNullOrEmpty(filePath))
        {
            throw new InvalidOperationException($"Could not get file path for file_id: {fileId}");
        }

        // Paso 2: descargar imagen
        Uri downloadUri = new($"https://api.telegram.org/file/bot{botToken}/{filePath}");
        using HttpResponseMessage imageResponse = await httpClient.GetAsync(downloadUri, cancellationToken).ConfigureAwait(false);
        imageResponse.EnsureSuccessStatusCode();

        await using Stream imageStream = await imageResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        // Paso 3: procesar y guardar con ImageSharp
        string productDir = Path.Combine(basePath, productId.ToString());
        Directory.CreateDirectory(productDir);

        using Image image = await Image.LoadAsync(imageStream, cancellationToken).ConfigureAwait(false);

        // Original
        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(1200, 900), Mode = ResizeMode.Max }));
        string originalFile = Path.Combine(productDir, $"original-{imageIndex}.webp");
        await image.SaveAsync(originalFile, WebpEncoder, cancellationToken).ConfigureAwait(false);

        // Medium
        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(600, 450), Mode = ResizeMode.Max }));
        string mediumFile = Path.Combine(productDir, $"medium-{imageIndex}.webp");
        await image.SaveAsync(mediumFile, WebpEncoder, cancellationToken).ConfigureAwait(false);

        // Thumb
        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(300, 225), Mode = ResizeMode.Max }));
        string thumbFile = Path.Combine(productDir, $"thumb-{imageIndex}.webp");
        await image.SaveAsync(thumbFile, WebpEncoder, cancellationToken).ConfigureAwait(false);

        return $"{baseUrl}/{productId}/original-{imageIndex}.webp";
    }
}
