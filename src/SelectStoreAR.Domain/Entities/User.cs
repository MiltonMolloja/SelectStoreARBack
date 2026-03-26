using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.Entities;

public sealed class User : BaseEntity
{
    private readonly List<ExternalLogin> _externalLogins = [];

    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;

    public string? PictureUrl { get; private set; }

    public string? Phone { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime LastLoginAt { get; private set; }

    public IReadOnlyList<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    public static User Create(string email, string name, string role, string? pictureUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required");
        }

        if (role is not ("user" or "admin"))
        {
            throw new DomainException("Role must be 'user' or 'admin'");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            // ToLowerInvariant is intentional for email normalization
            Email = email.ToLowerInvariant(),
            Name = name,
            Role = role,
            PictureUrl = pictureUrl,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        };
    }

    public void UpsertExternalLogin(string provider, string providerKey, string email, string name)
    {
        ExternalLogin? existing = _externalLogins
            .FirstOrDefault(l => l.Provider == provider && l.ProviderKey == providerKey);

        if (existing is null)
        {
            _externalLogins.Add(ExternalLogin.Create(Id, provider, providerKey, email, name));
        }
        else
        {
            existing.Update(email, name);
        }

        LastLoginAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, string? phone)
    {
        Name = name;
        Phone = phone;
    }

    public void UpdatePicture(string? pictureUrl)
    {
        PictureUrl = pictureUrl;
    }
}
