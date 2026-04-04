using SelectStoreAR.Application.Interfaces;

namespace SelectStoreAR.Infrastructure.Services;

/// <summary>
/// Cache no-op para Development — no cachea nada, siempre devuelve null.
/// Evita la dependencia de Redis en entornos locales.
/// </summary>
public sealed class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        where T : class => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
