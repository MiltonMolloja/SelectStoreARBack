using Microsoft.EntityFrameworkCore;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<User?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(
                u => u.ExternalLogins.Any(e => e.Provider == provider && e.ProviderKey == providerKey),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(User user) => dbContext.Users.Add(user);

    public void Update(User user) => dbContext.Users.Update(user);
}
