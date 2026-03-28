namespace SelectStoreAR.Application.Interfaces;

public interface IImageService
{
    /// <summary>
    /// Guarda una imagen en disco con 3 tamaños (original, medium, thumb).
    /// Usa el slug del producto como nombre de carpeta.
    /// </summary>
    Task<string> SaveImageAsync(
        Stream imageStream,
        string fileName,
        Guid productId,
        string? slug = null,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(string url, CancellationToken cancellationToken = default);

    Task DeleteProductImagesAsync(Guid productId, CancellationToken cancellationToken = default);
}
