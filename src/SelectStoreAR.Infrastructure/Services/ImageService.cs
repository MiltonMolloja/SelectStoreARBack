using Microsoft.Extensions.Configuration;
using SelectStoreAR.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace SelectStoreAR.Infrastructure.Services;

public sealed class ImageService(IConfiguration configuration) : IImageService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private static readonly WebpEncoder WebpEncoder = new() { Quality = 85, Method = WebpEncodingMethod.Level4 };

    private static readonly ResizeOptions OriginalOptions = new() { Size = new Size(1200, 900), Mode = ResizeMode.Max };

    private static readonly ResizeOptions MediumOptions = new() { Size = new Size(600, 450), Mode = ResizeMode.Max };

    private static readonly ResizeOptions ThumbOptions = new() { Size = new Size(300, 225), Mode = ResizeMode.Max };

    public async Task<string> SaveImageAsync(
        Stream imageStream,
        string fileName,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File type '{extension}' is not allowed. Use JPEG, PNG, WebP or GIF.");
        }

        string basePath = configuration["Images:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
        string productDir = Path.Combine(basePath, productId.ToString());
        Directory.CreateDirectory(productDir);

        string uniqueName = Guid.NewGuid().ToString("N");

        using Image image = await Image.LoadAsync(imageStream, cancellationToken).ConfigureAwait(false);

        // Original — max 1200x900
        image.Mutate(x => x.Resize(OriginalOptions));
        string originalPath = Path.Combine(productDir, $"original-{uniqueName}.webp");
        await image.SaveAsync(originalPath, WebpEncoder, cancellationToken).ConfigureAwait(false);

        // Medium — max 600x450
        image.Mutate(x => x.Resize(MediumOptions));
        string mediumPath = Path.Combine(productDir, $"medium-{uniqueName}.webp");
        await image.SaveAsync(mediumPath, WebpEncoder, cancellationToken).ConfigureAwait(false);

        // Thumbnail — max 300x225
        image.Mutate(x => x.Resize(ThumbOptions));
        string thumbPath = Path.Combine(productDir, $"thumb-{uniqueName}.webp");
        await image.SaveAsync(thumbPath, WebpEncoder, cancellationToken).ConfigureAwait(false);

        // Retorna la URL publica del original
        string baseUrl = configuration["Images:BaseUrl"] ?? "/images/products";
        return $"{baseUrl}/{productId}/original-{uniqueName}.webp";
    }

    public async Task DeleteImageAsync(string url, CancellationToken cancellationToken = default)
    {
        string basePath = configuration["Images:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
        string baseUrl = configuration["Images:BaseUrl"] ?? "/images/products";

        string relativePath = url.Replace(baseUrl, string.Empty, StringComparison.OrdinalIgnoreCase).TrimStart('/');
        string fileName = Path.GetFileNameWithoutExtension(relativePath);

        // Extraer nombre base (sin prefijo original-, medium-, thumb-)
        string baseName = fileName
            .Replace("original-", string.Empty, StringComparison.Ordinal)
            .Replace("medium-", string.Empty, StringComparison.Ordinal)
            .Replace("thumb-", string.Empty, StringComparison.Ordinal);

        string productDir = Path.GetDirectoryName(Path.Combine(basePath, relativePath)) ?? basePath;

        foreach (string prefix in new[] { "original", "medium", "thumb" })
        {
            string filePath = Path.Combine(productDir, $"{prefix}-{baseName}.webp");
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task DeleteProductImagesAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        string basePath = configuration["Images:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
        string productDir = Path.Combine(basePath, productId.ToString());

        if (Directory.Exists(productDir))
        {
            await Task.Run(() => Directory.Delete(productDir, recursive: true), cancellationToken).ConfigureAwait(false);
        }
    }
}
