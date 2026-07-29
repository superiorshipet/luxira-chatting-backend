using InternalChat.Application.Interfaces.Services;
using InternalChat.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace InternalChat.API.Services;

public class ChatNotifier : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyUserBlockedAsync(Guid userId)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync("UserBlocked");
    }

    public async Task NotifyMemberRemovedAsync(Guid groupId, Guid userId)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync("RemovedFromGroup", groupId);
    }
}
