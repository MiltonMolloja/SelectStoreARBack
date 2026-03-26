using Microsoft.EntityFrameworkCore;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(AppDbContext dbContext) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? categoryId = null,
        ProductStatus? status = null,
        string? searchQuery = null,
        decimal? minPriceUsd = null,
        decimal? maxPriceUsd = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = dbContext.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            string lowerQuery = searchQuery.ToLowerInvariant();
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{lowerQuery}%") ||
                EF.Functions.ILike(p.Brand, $"%{lowerQuery}%") ||
                EF.Functions.ILike(p.Description, $"%{lowerQuery}%"));
        }

        if (minPriceUsd.HasValue)
        {
            query = query.Where(p => p.BasePriceUsd.Amount >= minPriceUsd.Value);
        }

        if (maxPriceUsd.HasValue)
        {
            query = query.Where(p => p.BasePriceUsd.Amount <= maxPriceUsd.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<Product> items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(int limit = 8, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .Include(p => p.Images)
            .Where(p => p.IsFeatured && p.Status == ProductStatus.Active)
            .OrderByDescending(p => p.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = dbContext.Products.Where(p => p.Slug == slug);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TelegramMessageIdExistsAsync(string telegramMessageId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AnyAsync(p => p.TelegramMessageId == telegramMessageId, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(Product product) => dbContext.Products.Add(product);

    public void Update(Product product) => dbContext.Products.Update(product);

    public void Remove(Product product) => dbContext.Products.Remove(product);
}
