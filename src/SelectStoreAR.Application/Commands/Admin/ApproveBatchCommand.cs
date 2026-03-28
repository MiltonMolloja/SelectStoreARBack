using MediatR;
using Microsoft.Extensions.Logging;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Admin;

public sealed record ApproveBatchCommand(Guid BatchId, string ReviewedBy) : IRequest<ApproveBatchResult>;

public sealed record ApproveBatchResult(int Approved, int Skipped, IReadOnlyList<string> Errors);

public sealed class ApproveBatchHandler(
    IPendingChangeRepository pendingChangeRepository,
    IMediator mediator,
    ILogger<ApproveBatchHandler> logger)
    : IRequestHandler<ApproveBatchCommand, ApproveBatchResult>
{
    public async Task<ApproveBatchResult> Handle(ApproveBatchCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductPendingChange> changes = await pendingChangeRepository
            .GetByBatchAsync(request.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (changes.Count == 0)
        {
            return new ApproveBatchResult(0, 0, ["No changes found for this batch"]);
        }

        int approved = 0;
        int skipped = 0;
        List<string> errors = [];

        foreach (ProductPendingChange change in changes)
        {
            if (change.Status != PendingChangeStatus.Pending)
            {
                skipped++;
                continue;
            }

            ApprovePendingChangeResult result = await mediator
                .Send(new ApprovePendingChangeCommand(change.Id, request.ReviewedBy), cancellationToken)
                .ConfigureAwait(false);

            if (result.Success)
            {
                approved++;
            }
            else
            {
                errors.Add($"{change.ProposedName}: {result.Message}");
            }
        }

        logger.LogInformation(
            "Batch {BatchId} approved: {Approved} approved, {Skipped} skipped, {Errors} errors",
            request.BatchId,
            approved,
            skipped,
            errors.Count);

        return new ApproveBatchResult(approved, skipped, errors);
    }
}
