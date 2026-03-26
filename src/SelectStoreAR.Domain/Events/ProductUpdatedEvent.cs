using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.Events;

public sealed record ProductUpdatedEvent(Guid ProductId) : IDomainEvent;
