using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Domain.Interfaces;

public interface IPriceHistoryRepository
{
    Task<IReadOnlyList<PriceHistory>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);

    void Add(PriceHistory entry);
}
