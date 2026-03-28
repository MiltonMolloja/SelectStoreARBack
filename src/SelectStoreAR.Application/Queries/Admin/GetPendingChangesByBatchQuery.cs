using MediatR;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Admin;

public sealed record GetPendingChangesByBatchQuery(Guid BatchId) : IRequest<GetPendingChangesByBatchResult>;

public sealed record GetPendingChangesByBatchResult(IReadOnlyList<PendingChangeDto> Items);

public sealed class GetPendingChangesByBatchHandler(IPendingChangeRepository pendingChangeRepository)
    : IRequestHandler<GetPendingChangesByBatchQuery, GetPendingChangesByBatchResult>
{
    public async Task<GetPendingChangesByBatchResult> Handle(GetPendingChangesByBatchQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductPendingChange> items = await pendingChangeRepository
            .GetByBatchAsync(request.BatchId, cancellationToken)
            .ConfigureAwait(false);

        List<PendingChangeDto> dtos = items.Select(c => new PendingChangeDto(
            c.Id,
            c.ProductId,
            c.TelegramSyncBatchId,
            c.ChangeType.ToString(),
            c.Status.ToString(),
            c.ProposedName,
            c.ProposedBrand,
            c.ProposedPriceUsd.Amount,
            c.CurrentPriceUsd?.Amount,
            c.ProposedAvailability.ToString(),
            c.ProposedInspiration,
            c.ProposedCategory,
            c.RawTelegramText,
            c.ReviewedBy,
            c.ReviewedAt,
            c.ReviewNote,
            c.CreatedAt)).ToList();

        return new GetPendingChangesByBatchResult(dtos);
    }
}
