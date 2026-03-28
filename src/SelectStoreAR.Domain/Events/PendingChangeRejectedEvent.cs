using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.Events;

public sealed record PendingChangeRejectedEvent(
    Guid PendingChangeId,
    Guid? ProductId,
    string? Note) : IDomainEvent;
