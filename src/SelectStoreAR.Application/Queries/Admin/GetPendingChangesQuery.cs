using MediatR;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Admin;

public sealed record GetPendingChangesQuery(
    int Page = 1,
    int PageSize = 20,
    PendingChangeStatus? Status = null) : IRequest<GetPendingChangesResult>;

public sealed record GetPendingChangesResult(
    IReadOnlyList<PendingChangeDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PendingChangeDto(
    Guid Id,
    Guid? ProductId,
    Guid BatchId,
    string ChangeType,
    string Status,
    string ProposedName,
    string ProposedBrand,
    decimal ProposedPriceUsd,
    decimal? CurrentPriceUsd,
    string ProposedAvailability,
    string? ProposedInspiration,
    string ProposedCategory,
    string RawTelegramText,
    string? ReviewedBy,
    DateTime? ReviewedAt,
    string? ReviewNote,
    DateTime CreatedAt);

public sealed class GetPendingChangesHandler(IPendingChangeRepository pendingChangeRepository)
    : IRequestHandler<GetPendingChangesQuery, GetPendingChangesResult>
{
    public async Task<GetPendingChangesResult> Handle(GetPendingChangesQuery request, CancellationToken cancellationToken)
    {
        (IReadOnlyList<ProductPendingChange> items, int totalCount) = await pendingChangeRepository
            .GetPagedAsync(request.Page, request.PageSize, request.Status, cancellationToken)
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

        return new GetPendingChangesResult(dtos, totalCount, request.Page, request.PageSize);
    }
}
