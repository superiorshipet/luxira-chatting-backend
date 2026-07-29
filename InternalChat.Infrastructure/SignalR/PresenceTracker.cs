using InternalChat.Application.Interfaces.Services;
using StackExchange.Redis;

namespace InternalChat.Infrastructure.SignalR;

public class PresenceTracker : IPresenceTracker
{
    private readonly IDatabase _db;

    public PresenceTracker(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task ConnectionOpenedAsync(Guid userId, string connectionId)
    {
        await _db.SetAddAsync($"presence:{userId}", connectionId);
    }

    public async Task ConnectionClosedAsync(Guid userId, string connectionId)
    {
        await _db.SetRemoveAsync($"presence:{userId}", connectionId);
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId)
    {
        var count = await _db.SetLengthAsync($"presence:{userId}");
        return count > 0;
    }

    public async Task<IEnumerable<string>> GetConnectionsForUserAsync(Guid userId)
    {
        var members = await _db.SetMembersAsync($"presence:{userId}");
        return members.Select(m => m.ToString());
    }
}
