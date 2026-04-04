using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;

namespace SelectStoreAR.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? categoryId = null,
        ProductStatus? status = null,
        string? searchQuery = null,
        decimal? minPriceUsd = null,
        decimal? maxPriceUsd = null,
        string? brand = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetFeaturedAsync(int limit = 8, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<bool> TelegramMessageIdExistsAsync(string telegramMessageId, CancellationToken cancellationToken = default);

    void Add(Product product);

    void Update(Product product);

    void Remove(Product product);
}
