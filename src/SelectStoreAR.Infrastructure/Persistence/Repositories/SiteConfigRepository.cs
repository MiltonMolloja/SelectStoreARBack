using Microsoft.EntityFrameworkCore;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Infrastructure.Persistence.Repositories;

public sealed class SiteConfigRepository(AppDbContext dbContext) : ISiteConfigRepository
{
    public async Task<SiteConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await dbContext.SiteConfigs
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SiteConfig>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SiteConfigs
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(SiteConfig config) => dbContext.SiteConfigs.Add(config);

    public void Update(SiteConfig config) => dbContext.SiteConfigs.Update(config);
}
