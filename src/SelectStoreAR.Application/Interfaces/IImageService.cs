namespace SelectStoreAR.Application.Interfaces;

public interface IImageService
{
    Task<string> SaveImageAsync(
        Stream imageStream,
        string fileName,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(string url, CancellationToken cancellationToken = default);

    Task DeleteProductImagesAsync(Guid productId, CancellationToken cancellationToken = default);
}
