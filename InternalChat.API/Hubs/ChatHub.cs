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

    public async Task JoinGroup(Guid groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
    }

    public async Task LeaveGroup(Guid groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
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

    public async Task DeleteMessage(Guid groupId, Guid messageId)
    {
        var userId = GetUserId();
        await _messageService.DeleteMessageAsync(messageId, userId);
        await Clients.Group(groupId.ToString()).SendAsync("MessageDeleted", groupId, messageId, userId);
    }

    public async Task PinMessage(Guid groupId, Guid messageId, bool isPinned)
    {
        var userId = GetUserId();
        await _messageService.PinMessageAsync(messageId, userId, isPinned);
        await Clients.Group(groupId.ToString()).SendAsync("MessagePinned", groupId, messageId, isPinned);
    }

    public async Task UserTyping(Guid groupId, bool isTyping)
    {
        var userId = GetUserId();
        await Clients.GroupExcept(groupId.ToString(), Context.ConnectionId)
            .SendAsync("UserTyping", groupId, userId, isTyping);
    }

    // ─────────────── WebRTC Signaling (Voice & Video Calls) ───────────────
    // The Admin initiates calls. Peers signal each other through the server.

    /// <summary>Initiate a call to a group or specific user. Sends an offer SDP.</summary>
    public async Task CallOffer(Guid targetGroupId, string sdpOffer, bool isVideo)
    {
        var callerId = GetUserId();
        await Clients.Group(targetGroupId.ToString())
            .SendAsync("IncomingCall", callerId, sdpOffer, isVideo, targetGroupId);
    }

    /// <summary>Answer a call with an SDP answer.</summary>
    public async Task CallAnswer(Guid targetGroupId, Guid callerId, string sdpAnswer)
    {
        // Send answer back to the caller's group connection
        await Clients.Group(targetGroupId.ToString())
            .SendAsync("CallAnswered", GetUserId(), sdpAnswer);
    }

    /// <summary>Exchange ICE candidates for NAT traversal.</summary>
    public async Task SendIceCandidate(Guid targetGroupId, string candidate)
    {
        await Clients.GroupExcept(targetGroupId.ToString(), Context.ConnectionId)
            .SendAsync("IceCandidate", GetUserId(), candidate);
    }

    /// <summary>End an ongoing call.</summary>
    public async Task EndCall(Guid targetGroupId)
    {
        await Clients.Group(targetGroupId.ToString())
            .SendAsync("CallEnded", GetUserId());
    }
}
