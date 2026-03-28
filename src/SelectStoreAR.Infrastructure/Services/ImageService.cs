using Microsoft.Extensions.Configuration;
using SelectStoreAR.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace SelectStoreAR.Infrastructure.Services;

/// <summary>
/// Guarda imágenes de productos en disco local con 3 tamaños (original, medium, thumb).
/// Estructura: /uploads/products/{slug}/{original|medium|thumb}-{uniqueId}.webp
/// </summary>
public sealed class ImageService(IConfiguration configuration) : IImageService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private static readonly WebpEncoder WebpEncoder = new() { Quality = 85, Method = WebpEncodingMethod.Level4 };

    private static readonly ResizeOptions OriginalOptions = new() { Size = new Size(2048, 2048), Mode = ResizeMode.Max };

    private static readonly ResizeOptions MediumOptions = new() { Size = new Size(800, 800), Mode = ResizeMode.Max };

    private static readonly ResizeOptions ThumbOptions = new() { Size = new Size(300, 300), Mode = ResizeMode.Max };

    private string BasePath => configuration["Images:BasePath"]
        ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads", "products");

    private string BaseUrl => configuration["Images:BaseUrl"] ?? "/uploads/products";

    public async Task<string> SaveImageAsync(
        Stream imageStream,
        string fileName,
        Guid productId,
        string? slug = null,
        CancellationToken cancellationToken = default)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File type '{extension}' is not allowed. Use JPEG, PNG, WebP or GIF.");
        }

        // Usar slug si está disponible, sino productId
        string folderName = slug ?? productId.ToString();
        string productDir = Path.Combine(BasePath, folderName);
        Directory.CreateDirectory(productDir);

        string uniqueName = Guid.NewGuid().ToString("N")[..8];

        using Image image = await Image.LoadAsync(imageStream, cancellationToken).ConfigureAwait(false);

        // Original — max 2048px
        using Image original = image.Clone(x => x.Resize(OriginalOptions));
        string originalPath = Path.Combine(productDir, $"original-{uniqueName}.webp");
        await original.SaveAsync(originalPath, WebpEncoder, cancellationToken).ConfigureAwait(false);

        // Medium — max 800px
        using Image medium = image.Clone(x => x.Resize(MediumOptions));
        string mediumPath = Path.Combine(productDir, $"medium-{uniqueName}.webp");
        await medium.SaveAsync(mediumPath, WebpEncoder, cancellationToken).ConfigureAwait(false);

        // Thumbnail — max 300px
        using Image thumb = image.Clone(x => x.Resize(ThumbOptions));
        string thumbPath = Path.Combine(productDir, $"thumb-{uniqueName}.webp");
        await thumb.SaveAsync(thumbPath, WebpEncoder, cancellationToken).ConfigureAwait(false);

        return $"{BaseUrl}/{folderName}/original-{uniqueName}.webp";
    }

    public async Task<string> SaveImageAsync(
        Stream imageStream,
        string fileName,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await SaveImageAsync(imageStream, fileName, productId, slug: null, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteImageAsync(string url, CancellationToken cancellationToken = default)
    {
        string relativePath = url.Replace(BaseUrl, string.Empty, StringComparison.OrdinalIgnoreCase).TrimStart('/');
        string fileName = Path.GetFileNameWithoutExtension(relativePath);

        string baseName = fileName
            .Replace("original-", string.Empty, StringComparison.Ordinal)
            .Replace("medium-", string.Empty, StringComparison.Ordinal)
            .Replace("thumb-", string.Empty, StringComparison.Ordinal);

        string productDir = Path.GetDirectoryName(Path.Combine(BasePath, relativePath)) ?? BasePath;

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
        // Intentar borrar por GUID y por slug
        string guidDir = Path.Combine(BasePath, productId.ToString());
        if (Directory.Exists(guidDir))
        {
            await Task.Run(() => Directory.Delete(guidDir, recursive: true), cancellationToken).ConfigureAwait(false);
        }
    }
}
