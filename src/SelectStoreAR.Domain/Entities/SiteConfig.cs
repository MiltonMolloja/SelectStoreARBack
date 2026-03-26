using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.Entities;

public sealed class SiteConfig : BaseEntity
{
    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public DateTime UpdatedAt { get; private set; }

    private SiteConfig()
    {
    }

    public static SiteConfig Create(string key, string value)
    {
        return new SiteConfig
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
