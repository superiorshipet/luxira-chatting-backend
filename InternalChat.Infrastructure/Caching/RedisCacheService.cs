using System.Text.Json;
using InternalChat.Application.Interfaces.Services;
using StackExchange.Redis;

namespace InternalChat.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly IServer _server;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints().First());
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue) return default;
        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        var json = JsonSerializer.Serialize(value);
        if (ttl.HasValue)
        {
            await _db.StringSetAsync(key, json, ttl.Value);
        }
        else
        {
            await _db.StringSetAsync(key, json);
        }
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        var keys = _server.Keys(pattern: $"{prefix}*").ToArray();
        if (keys.Any())
        {
            await _db.KeyDeleteAsync(keys);
        }
    }
}
