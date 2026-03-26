using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;

namespace SelectStoreAR.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        OrderStatus? status = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(Order order);

    void Update(Order order);
}
