using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.Events;

public sealed record ProductCreatedEvent(Guid ProductId) : IDomainEvent;
