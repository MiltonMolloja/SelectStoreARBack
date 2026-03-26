using Microsoft.EntityFrameworkCore;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Infrastructure.Persistence.Repositories;

public sealed class ExchangeRateRepository(AppDbContext dbContext) : IExchangeRateRepository
{
    public async Task<ExchangeRate?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ExchangeRates
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(ExchangeRate exchangeRate) => dbContext.ExchangeRates.Add(exchangeRate);

    public void Update(ExchangeRate exchangeRate) => dbContext.ExchangeRates.Update(exchangeRate);
}
