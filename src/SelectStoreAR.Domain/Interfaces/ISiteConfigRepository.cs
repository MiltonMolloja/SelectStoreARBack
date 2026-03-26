using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Domain.Interfaces;

public interface ISiteConfigRepository
{
    Task<SiteConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiteConfig>> GetAllAsync(CancellationToken cancellationToken = default);

    void Add(SiteConfig config);

    void Update(SiteConfig config);
}
