using InternalChat.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InternalChat.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IPresenceTracker _presenceTracker;
    private readonly IGroupService _groupService;

    public ChatHub(IPresenceTracker presenceTracker, IGroupService groupService)
    {
        _presenceTracker = presenceTracker;
        _groupService = groupService;
    }

    private Guid GetUserId()
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdStr!);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        
        var userGroups = await _groupService.GetUserGroupsAsync(userId);
        foreach (var group in userGroups)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group.Id.ToString());
        }

        await _presenceTracker.ConnectionOpenedAsync(userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        await _presenceTracker.ConnectionClosedAsync(userId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
