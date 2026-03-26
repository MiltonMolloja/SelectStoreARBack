using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    void Add(Category category);

    void Update(Category category);

    void Remove(Category category);
}
