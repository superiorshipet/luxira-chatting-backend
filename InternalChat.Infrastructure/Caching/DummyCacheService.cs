using InternalChat.Application.Interfaces.Services;

namespace InternalChat.Infrastructure.Caching;

public class DummyCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) => Task.FromResult(default(T));
    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) => Task.CompletedTask;
    public Task RemoveAsync(string key) => Task.CompletedTask;
    public Task RemoveByPrefixAsync(string prefix) => Task.CompletedTask;
}
