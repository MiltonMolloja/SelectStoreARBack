using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.Entities;

public sealed class ExternalLogin
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string ProviderKey { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private ExternalLogin()
    {
    }

    public static ExternalLogin Create(
        Guid userId,
        string provider,
        string providerKey,
        string email,
        string name)
    {
        if (provider is not ("Google" or "Facebook"))
        {
            throw new DomainException("Provider must be 'Google' or 'Facebook'");
        }

        return new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderKey = providerKey,
            Email = email,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string email, string name)
    {
        Email = email;
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
