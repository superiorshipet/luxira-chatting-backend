using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternalChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IMessageService _messageService;

    public GroupController(IGroupService groupService, IMessageService messageService)
    {
        _groupService   = groupService;
        _messageService = messageService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get all groups the user belongs to, with optional filter: "unread" | "favorites" | (none for all)</summary>
    [HttpGet("my-groups")]
    public async Task<IActionResult> GetMyGroups([FromQuery] string? filter)
    {
        var groups = await _groupService.GetUserGroupsFilteredAsync(GetUserId(), filter);
        return Ok(groups);
    }

    /// <summary>Get the members of a group.</summary>
    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetGroupMembers(Guid groupId)
    {
        try
        {
            var members = await _groupService.GetGroupMembersAsync(groupId, GetUserId());
            return Ok(members);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Get paginated message history for a group.</summary>
    [HttpGet("{groupId}/messages")]
    public async Task<IActionResult> GetMessages(Guid groupId, [FromQuery] DateTime? beforeCursor, [FromQuery] int take = 50)
    {
        try
        {
            var cursor   = beforeCursor ?? DateTime.UtcNow;
            var messages = await _messageService.GetMessagesAsync(groupId, GetUserId(), cursor, take);
            return Ok(messages);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Search for a keyword across all messages in groups the user belongs to.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchMessages([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { error = "Keyword is required." });

        var results = await _groupService.SearchMessagesAsync(GetUserId(), keyword);
        return Ok(results);
    }

    /// <summary>Mark all messages in a group as read for the current user.</summary>
    [HttpPost("{groupId}/mark-read")]
    public async Task<IActionResult> MarkGroupAsRead(Guid groupId)
    {
        await _messageService.MarkGroupAsReadAsync(groupId, GetUserId());
        return Ok(new { message = "Marked as read." });
    }
}
