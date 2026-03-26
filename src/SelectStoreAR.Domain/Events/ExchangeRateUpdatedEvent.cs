using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.Events;

public sealed record ExchangeRateUpdatedEvent(Guid RateId, decimal NewRate) : IDomainEvent;
