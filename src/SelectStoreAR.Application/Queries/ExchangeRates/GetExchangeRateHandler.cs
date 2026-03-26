using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.ExchangeRates;

public sealed class GetExchangeRateHandler(IExchangeRateRepository exchangeRateRepository)
    : IRequestHandler<GetExchangeRateQuery, ExchangeRateDto?>
{
    public async Task<ExchangeRateDto?> Handle(GetExchangeRateQuery request, CancellationToken cancellationToken)
    {
        ExchangeRate? rate = await exchangeRateRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        if (rate is null)
        {
            return null;
        }

        return new ExchangeRateDto(rate.Id, rate.Rate, rate.Type, rate.IsActive, rate.IsStale(), rate.UpdatedAt);
    }
}
