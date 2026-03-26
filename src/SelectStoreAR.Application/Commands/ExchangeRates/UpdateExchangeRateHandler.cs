using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.ExchangeRates;

public sealed class UpdateExchangeRateHandler(
    IExchangeRateRepository exchangeRateRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateExchangeRateCommand, ExchangeRateDto>
{
    public async Task<ExchangeRateDto> Handle(UpdateExchangeRateCommand request, CancellationToken cancellationToken)
    {
        ExchangeRate? existing = await exchangeRateRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Update(request.Rate);
            exchangeRateRepository.Update(existing);
        }
        else
        {
            ExchangeRate newRate = ExchangeRate.Create(request.Rate, request.Type);
            exchangeRateRepository.Add(newRate);
            existing = newRate;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ExchangeRateDto(
            existing.Id,
            existing.Rate,
            existing.Type,
            existing.IsActive,
            existing.IsStale(),
            existing.UpdatedAt);
    }
}
