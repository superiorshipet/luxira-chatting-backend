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
        _groupService = groupService;
        _messageService = messageService;
    }
    
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("my-groups")]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = GetUserId();
        var groups = await _groupService.GetUserGroupsAsync(userId);
        return Ok(groups);
    }

    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetGroupMembers(Guid groupId)
    {
        try
        {
            var userId = GetUserId();
            var members = await _groupService.GetGroupMembersAsync(groupId, userId);
            return Ok(members);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{groupId}/messages")]
    public async Task<IActionResult> GetMessages(Guid groupId, [FromQuery] DateTime? beforeCursor, [FromQuery] int take = 50)
    {
        try
        {
            var userId = GetUserId();
            var cursor = beforeCursor ?? DateTime.UtcNow;
            var messages = await _messageService.GetMessagesAsync(groupId, userId, cursor, take);
            return Ok(messages);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{groupId}/members/me/mute")]
    public async Task<IActionResult> MuteMember(Guid groupId, [FromBody] MuteMemberRequest request)
    {
        try
        {
            var userId = GetUserId();
            await _groupService.MuteMemberAsync(groupId, userId, request.IsMuted);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
