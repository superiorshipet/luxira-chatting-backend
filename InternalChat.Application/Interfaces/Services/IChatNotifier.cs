namespace InternalChat.Application.Interfaces.Services;

/// <summary>
/// Interface for sending real-time notifications to users and groups.
/// This abstracts the SignalR hub context away from application services.
/// </summary>
public interface IChatNotifier
{
    Task NotifyUserBlockedAsync(Guid userId);
    Task NotifyMemberRemovedAsync(Guid groupId, Guid userId);
}
