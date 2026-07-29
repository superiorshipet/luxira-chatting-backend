using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternalChat.API.Controllers;

/// <summary>
/// All Admin-only management endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly Infrastructure.Persistence.AppDbContext _db;

    public AdminController(IUserService userService, IGroupService groupService, Infrastructure.Persistence.AppDbContext db)
    {
        _userService  = userService;
        _groupService = groupService;
        _db           = db;
    }

    private Guid AdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─────────────────── USER MANAGEMENT ───────────────────

    /// <summary>List all employees in the system.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
        => Ok(await _userService.GetAllUsersAsync());

    /// <summary>Create a new employee account.</summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userService.CreateUserAsync(
                request.PhoneNumber, request.Password, request.FullName, request.Email, AdminId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Block an employee.</summary>
    [HttpPost("users/{userId}/block")]
    public async Task<IActionResult> BlockUser(Guid userId, [FromBody] BlockRequest? request)
    {
        await _userService.BlockUserAsync(userId, AdminId, request?.Reason);
        return Ok(new { message = "User blocked." });
    }

    /// <summary>Unblock an employee.</summary>
    [HttpPost("users/{userId}/unblock")]
    public async Task<IActionResult> UnblockUser(Guid userId)
    {
        await _userService.UnblockUserAsync(userId);
        return Ok(new { message = "User unblocked." });
    }

    /// <summary>Toggle the Facebook-style verification badge for a user.</summary>
    [HttpPost("users/{userId}/verify")]
    public async Task<IActionResult> ToggleVerification(Guid userId)
    {
        await _userService.ToggleVerificationAsync(userId, AdminId);
        return Ok(new { message = "Verification badge toggled." });
    }

    /// <summary>Grant or revoke permission for a user to send private messages to the admin.</summary>
    [HttpPut("users/{userId}/private-permission")]
    public async Task<IActionResult> SetPrivatePermission(Guid userId, [FromBody] GrantPrivatePermissionRequest request)
    {
        await _userService.GrantPrivateMessagePermissionAsync(userId, AdminId, request.Grant);
        return Ok(new { message = $"Private message permission {(request.Grant ? "granted" : "revoked")}." });
    }

    // ─────────────────── GROUP MANAGEMENT ───────────────────

    /// <summary>Create a new group chat.</summary>
    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var group = await _groupService.CreateGroupAsync(request.Name, request.ImageUrl, AdminId);
        return Ok(group);
    }

    /// <summary>Get all groups in the system (Admin only).</summary>
    [HttpGet("groups")]
    public async Task<IActionResult> GetAllGroups()
    {
        var groups = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _db.Groups.Where(g => !g.IsArchived && !g.IsPrivate));
        return Ok(groups.Select(g => new GroupDto(g.Id, g.Name, g.ImageUrl, g.CreatedAt)));
    }

    /// <summary>Create a private 1-on-1 chat with a specific employee.</summary>
    [HttpPost("groups/private")]
    public async Task<IActionResult> CreatePrivateChat([FromBody] CreatePrivateChatRequest request)
    {
        try
        {
            var group = await _userService.CreatePrivateChatAsync(AdminId, request.TargetUserId);
            return Ok(group);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Add members to a group.</summary>
    [HttpPost("groups/{groupId}/members")]
    public async Task<IActionResult> AddMembers(Guid groupId, [FromBody] AddMembersRequest request)
    {
        await _groupService.AddMembersAsync(groupId, request.UserIds, AdminId);
        return Ok(new { message = "Members added." });
    }

    /// <summary>Remove a member from a group.</summary>
    [HttpDelete("groups/{groupId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId)
    {
        await _groupService.RemoveMemberAsync(groupId, userId);
        return Ok(new { message = "Member removed." });
    }

    /// <summary>Mute/unmute a member in a group (also used for private chat reply permission).</summary>
    [HttpPut("groups/{groupId}/members/{userId}/mute")]
    public async Task<IActionResult> MuteMember(Guid groupId, Guid userId, [FromBody] MuteMemberRequest request)
    {
        await _groupService.MuteMemberAsync(groupId, userId, request.IsMuted);
        return Ok(new { message = $"User {(request.IsMuted ? "muted" : "unmuted")}." });
    }

    /// <summary>Update group details (name, image).</summary>
    [HttpPut("groups/{groupId}")]
    public async Task<IActionResult> UpdateGroup(Guid groupId, [FromBody] UpdateGroupRequest request)
    {
        try
        {
            await _groupService.UpdateGroupAsync(groupId, request.Name, request.ImageUrl);
            return Ok(new { message = "Group updated successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record BlockRequest(string? Reason);
public record UpdateGroupRequest(string Name, string? ImageUrl);
