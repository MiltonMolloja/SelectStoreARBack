using Microsoft.EntityFrameworkCore;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .Include(c => c.Children)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .Include(c => c.Children)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .Include(c => c.Children)
            .Include(c => c.Products)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .Include(c => c.Children)
            .ThenInclude(c => c.Products)
            .Include(c => c.Products)
            .Where(c => c.ParentId == null)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Category> query = dbContext.Categories.Where(c => c.Slug == slug);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Add(Category category) => dbContext.Categories.Add(category);

    public void Update(Category category) => dbContext.Categories.Update(category);

    public void Remove(Category category) => dbContext.Categories.Remove(category);
}
