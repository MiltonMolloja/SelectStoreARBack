using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Domain.Interfaces;

public interface IExchangeRateRepository
{
    Task<ExchangeRate?> GetActiveAsync(CancellationToken cancellationToken = default);

    void Add(ExchangeRate exchangeRate);

    void Update(ExchangeRate exchangeRate);
}
