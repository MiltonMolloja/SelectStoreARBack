using Microsoft.EntityFrameworkCore;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        OrderStatus? status = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = dbContext.Orders
            .Include(o => o.Items)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<Order> items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(Order order) => dbContext.Orders.Add(order);

    public void Update(Order order) => dbContext.Orders.Update(order);
}
