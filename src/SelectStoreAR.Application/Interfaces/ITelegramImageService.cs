namespace SelectStoreAR.Application.Interfaces;

public interface ITelegramImageService
{
    Task<string> DownloadAndSaveAsync(
        string fileId,
        Guid productId,
        int imageIndex,
        CancellationToken cancellationToken = default);
}
