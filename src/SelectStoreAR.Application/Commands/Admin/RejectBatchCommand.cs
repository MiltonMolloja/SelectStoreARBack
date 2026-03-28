using MediatR;
using Microsoft.Extensions.Logging;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Admin;

public sealed record RejectBatchCommand(Guid BatchId, string ReviewedBy, string? Note = null) : IRequest<RejectBatchResult>;

public sealed record RejectBatchResult(int Rejected, int Skipped);

public sealed class RejectBatchHandler(
    IPendingChangeRepository pendingChangeRepository,
    IUnitOfWork unitOfWork,
    ILogger<RejectBatchHandler> logger)
    : IRequestHandler<RejectBatchCommand, RejectBatchResult>
{
    public async Task<RejectBatchResult> Handle(RejectBatchCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductPendingChange> changes = await pendingChangeRepository
            .GetByBatchAsync(request.BatchId, cancellationToken)
            .ConfigureAwait(false);

        int rejected = 0;
        int skipped = 0;

        foreach (ProductPendingChange change in changes)
        {
            if (change.Status != PendingChangeStatus.Pending)
            {
                skipped++;
                continue;
            }

            change.Reject(request.ReviewedBy, request.Note);
            rejected++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Batch {BatchId} rejected: {Rejected} rejected, {Skipped} skipped",
            request.BatchId,
            rejected,
            skipped);

        return new RejectBatchResult(rejected, skipped);
    }
}
