using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Services;
using InternalChat.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InternalChat.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IPresenceTracker _presenceTracker;
    private readonly IGroupService _groupService;
    private readonly IMessageService _messageService;

    public ChatHub(IPresenceTracker presenceTracker, IGroupService groupService, IMessageService messageService)
    {
        _presenceTracker = presenceTracker;
        _groupService = groupService;
        _messageService = messageService;
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
        
        // Notify others that user came online
        await Clients.Others.SendAsync("UserPresenceChanged", userId, true);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        await _presenceTracker.ConnectionClosedAsync(userId, Context.ConnectionId);
        
        var isOnline = await _presenceTracker.IsUserOnlineAsync(userId);
        if (!isOnline)
        {
            await Clients.Others.SendAsync("UserPresenceChanged", userId, false);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(Guid groupId, string? content, MessageType type, string? attachmentUrl, Guid? replyToMessageId)
    {
        var userId = GetUserId();
        var message = await _messageService.SendMessageAsync(groupId, userId, content, type, attachmentUrl, replyToMessageId);
        await Clients.Group(groupId.ToString()).SendAsync("ReceiveMessage", message);
    }

    public async Task EditMessage(Guid messageId, string newContent)
    {
        var userId = GetUserId();
        var message = await _messageService.EditMessageAsync(messageId, userId, newContent);
        await Clients.Group(message.GroupId.ToString()).SendAsync("MessageEdited", message);
    }

    public async Task MarkAsRead(Guid groupId, Guid messageId)
    {
        var userId = GetUserId();
        await _messageService.MarkAsReadAsync(messageId, userId);
        await Clients.Group(groupId.ToString()).SendAsync("MessageRead", groupId, messageId, userId);
    }

    public async Task ReactToMessage(Guid groupId, Guid messageId, string emoji)
    {
        var userId = GetUserId();
        await _messageService.ReactToMessageAsync(messageId, userId, emoji);
        await Clients.Group(groupId.ToString()).SendAsync("MessageReacted", groupId, messageId, userId, emoji);
    }

    public async Task ForwardMessage(Guid messageId, IEnumerable<Guid> targetGroupIds)
    {
        var userId = GetUserId();
        var forwardedMessages = await _messageService.ForwardMessageAsync(messageId, userId, targetGroupIds);
        
        foreach (var msg in forwardedMessages)
        {
            await Clients.Group(msg.GroupId.ToString()).SendAsync("ReceiveMessage", msg);
        }
    }
}
