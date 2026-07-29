namespace InternalChat.Application.Interfaces.Services;

/// <summary>
/// Thin abstraction over the distributed cache so Services/Hub never talk to
/// StackExchange.Redis directly — keeps Infrastructure swappable and keeps
/// cache-key conventions in one place.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}
