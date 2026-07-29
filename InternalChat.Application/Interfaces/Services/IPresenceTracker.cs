namespace InternalChat.Application.Interfaces.Services;

/// <summary>
/// Tracks active SignalR connections and online presence for users.
/// </summary>
public interface IPresenceTracker
{
    Task ConnectionOpenedAsync(Guid userId, string connectionId);
    Task ConnectionClosedAsync(Guid userId, string connectionId);
    Task<bool> IsUserOnlineAsync(Guid userId);
    Task<IEnumerable<string>> GetConnectionsForUserAsync(Guid userId);
}
