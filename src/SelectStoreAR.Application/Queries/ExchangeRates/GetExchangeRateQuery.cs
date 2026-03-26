using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Queries.ExchangeRates;

public sealed record GetExchangeRateQuery : IRequest<ExchangeRateDto?>;
