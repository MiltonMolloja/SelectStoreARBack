using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SelectStoreAR.Application.Interfaces;
using StackExchange.Redis;

namespace SelectStoreAR.Infrastructure.Services;

public sealed class CacheService(IDistributedCache cache, IConnectionMultiplexer redis) : ICacheService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        byte[]? data = await cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (data is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(data, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        where T : class
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5),
        };

        await cache.SetAsync(key, data, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        IServer server = redis.GetServer(redis.GetEndPoints().First());
        IAsyncEnumerable<RedisKey> keys = server.KeysAsync(pattern: pattern);

        await foreach (RedisKey key in keys.WithCancellation(cancellationToken))
        {
            await cache.RemoveAsync(key.ToString(), cancellationToken).ConfigureAwait(false);
        }
    }
}
