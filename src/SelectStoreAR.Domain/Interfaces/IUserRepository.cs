using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default);

    void Add(User user);

    void Update(User user);
}
