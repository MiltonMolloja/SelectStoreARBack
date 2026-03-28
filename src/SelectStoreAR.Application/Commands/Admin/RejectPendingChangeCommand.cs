using MediatR;
using Microsoft.Extensions.Logging;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Admin;

public sealed record RejectPendingChangeCommand(Guid ChangeId, string ReviewedBy, string? Note = null) : IRequest<RejectPendingChangeResult>;

public sealed record RejectPendingChangeResult(bool Success, string Message);

public sealed class RejectPendingChangeHandler(
    IPendingChangeRepository pendingChangeRepository,
    IUnitOfWork unitOfWork,
    ILogger<RejectPendingChangeHandler> logger)
    : IRequestHandler<RejectPendingChangeCommand, RejectPendingChangeResult>
{
    public async Task<RejectPendingChangeResult> Handle(RejectPendingChangeCommand request, CancellationToken cancellationToken)
    {
        ProductPendingChange? change = await pendingChangeRepository
            .GetByIdAsync(request.ChangeId, cancellationToken)
            .ConfigureAwait(false);

        if (change is null)
        {
            return new RejectPendingChangeResult(false, "Pending change not found");
        }

        if (change.Status != PendingChangeStatus.Pending)
        {
            return new RejectPendingChangeResult(false, $"Change is already {change.Status}");
        }

        change.Reject(request.ReviewedBy, request.Note);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Rejected change {Id} for {Name}: {Note}", change.Id, change.ProposedName, request.Note);

        return new RejectPendingChangeResult(true, "Rejected");
    }
}
