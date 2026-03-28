using Microsoft.EntityFrameworkCore;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Infrastructure.Persistence.Repositories;

public sealed class PriceHistoryRepository(AppDbContext dbContext) : IPriceHistoryRepository
{
    public async Task<IReadOnlyList<PriceHistory>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PriceHistories
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.ChangedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(PriceHistory entry) => dbContext.PriceHistories.Add(entry);
}
