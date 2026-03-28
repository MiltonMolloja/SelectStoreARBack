using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;

namespace SelectStoreAR.Domain.Interfaces;

public interface IPendingChangeRepository
{
    Task<ProductPendingChange?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el cambio pendiente activo para un producto (solo puede haber uno).
    /// </summary>
    Task<ProductPendingChange?> GetPendingByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el cambio pendiente para un producto nuevo por slug propuesto.
    /// </summary>
    Task<ProductPendingChange?> GetPendingByProposedNameAsync(string proposedName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todos los cambios de un mismo batch (mensaje de Telegram).
    /// </summary>
    Task<IReadOnlyList<ProductPendingChange>> GetByBatchAsync(Guid batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista paginada de cambios filtrados por estado.
    /// </summary>
    Task<(IReadOnlyList<ProductPendingChange> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        PendingChangeStatus? status = null,
        CancellationToken cancellationToken = default);

    void Add(ProductPendingChange change);

    void Update(ProductPendingChange change);

    void Remove(ProductPendingChange change);
}
