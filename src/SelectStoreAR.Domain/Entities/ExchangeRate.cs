using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Events;

namespace SelectStoreAR.Domain.Entities;

public sealed class ExchangeRate : BaseEntity
{
    public Guid Id { get; private set; }

    public decimal Rate { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public string UpdatedBy { get; private set; } = string.Empty;

    private ExchangeRate()
    {
    }

    public static ExchangeRate Create(decimal rate, string type = "blue")
    {
        if (rate <= 0)
        {
            throw new DomainException("Exchange rate must be positive");
        }

        return new ExchangeRate
        {
            Id = Guid.NewGuid(),
            Rate = rate,
            Type = type,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "admin",
        };
    }

    public void Update(decimal newRate)
    {
        if (newRate <= 0)
        {
            throw new DomainException("Exchange rate must be positive");
        }

        Rate = newRate;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ExchangeRateUpdatedEvent(Id, newRate));
    }

    public bool IsStale() => (DateTime.UtcNow - UpdatedAt).TotalHours > 24;
}
