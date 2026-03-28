using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Enums;

namespace SelectStoreAR.Domain.Events;

public sealed record PendingChangeApprovedEvent(
    Guid PendingChangeId,
    Guid? ProductId,
    PendingChangeType ChangeType) : IDomainEvent;
