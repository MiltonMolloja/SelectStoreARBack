using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.Events;

public sealed record OrderPlacedEvent(Guid OrderId, string OrderNumber) : IDomainEvent;
