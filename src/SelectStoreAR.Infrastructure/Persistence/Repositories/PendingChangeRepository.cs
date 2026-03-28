using Microsoft.EntityFrameworkCore;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Infrastructure.Persistence.Repositories;

public sealed class PendingChangeRepository(AppDbContext dbContext) : IPendingChangeRepository
{
    public async Task<ProductPendingChange?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductPendingChanges
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ProductPendingChange?> GetPendingByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductPendingChanges
            .FirstOrDefaultAsync(
                p => p.ProductId == productId && p.Status == PendingChangeStatus.Pending,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ProductPendingChange?> GetPendingByProposedNameAsync(string proposedName, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductPendingChanges
            .FirstOrDefaultAsync(
                p => p.ProposedName == proposedName
                     && p.Status == PendingChangeStatus.Pending
                     && p.ProductId == null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProductPendingChange>> GetByBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductPendingChanges
            .Include(p => p.Product)
            .Where(p => p.TelegramSyncBatchId == batchId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<ProductPendingChange> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        PendingChangeStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ProductPendingChange> query = dbContext.ProductPendingChanges
            .Include(p => p.Product)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<ProductPendingChange> items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public void Add(ProductPendingChange change) => dbContext.ProductPendingChanges.Add(change);

    public void Update(ProductPendingChange change) => dbContext.ProductPendingChanges.Update(change);

    public void Remove(ProductPendingChange change) => dbContext.ProductPendingChanges.Remove(change);
}
