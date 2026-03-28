using MediatR;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Products;

public sealed record GetPriceHistoryQuery(Guid ProductId) : IRequest<GetPriceHistoryResult>;

public sealed record GetPriceHistoryResult(IReadOnlyList<PriceHistoryDto> Items);

public sealed record PriceHistoryDto(
    Guid Id,
    decimal PriceUsd,
    string Source,
    Guid? OrderId,
    string? ChangedBy,
    DateTime ChangedAt);

public sealed class GetPriceHistoryHandler(IPriceHistoryRepository priceHistoryRepository)
    : IRequestHandler<GetPriceHistoryQuery, GetPriceHistoryResult>
{
    public async Task<GetPriceHistoryResult> Handle(GetPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<PriceHistory> items = await priceHistoryRepository
            .GetByProductAsync(request.ProductId, cancellationToken)
            .ConfigureAwait(false);

        List<PriceHistoryDto> dtos = items.Select(p => new PriceHistoryDto(
            p.Id,
            p.PriceUsd.Amount,
            p.Source.ToString(),
            p.OrderId,
            p.ChangedBy,
            p.ChangedAt)).ToList();

        return new GetPriceHistoryResult(dtos);
    }
}
