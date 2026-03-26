using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Commands.ExchangeRates;

public sealed record UpdateExchangeRateCommand(decimal Rate, string Type = "blue") : IRequest<ExchangeRateDto>;
