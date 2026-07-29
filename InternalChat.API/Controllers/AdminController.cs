using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternalChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;

    public AdminController(IUserService userService, IGroupService groupService)
    {
        _userService = userService;
        _groupService = groupService;
    }
    
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // USERS

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var adminId = GetUserId();
            var user = await _userService.CreateUserAsync(request.PhoneNumber, request.Password, request.FullName, adminId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("users/{userId}/block")]
    public async Task<IActionResult> BlockUser(Guid userId, [FromQuery] string? reason)
    {
        try
        {
            var adminId = GetUserId();
            await _userService.BlockUserAsync(userId, adminId, reason);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("users/{userId}/unblock")]
    public async Task<IActionResult> UnblockUser(Guid userId)
    {
        try
        {
            await _userService.UnblockUserAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GROUPS

    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        try
        {
            var adminId = GetUserId();
            var group = await _groupService.CreateGroupAsync(request.Name, request.ImageUrl, adminId);
            return Ok(group);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("groups/{groupId}/members")]
    public async Task<IActionResult> AddGroupMembers(Guid groupId, [FromBody] AddMembersRequest request)
    {
        try
        {
            var adminId = GetUserId();
            await _groupService.AddMembersAsync(groupId, request.UserIds, adminId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("groups/{groupId}/members/{userId}")]
    public async Task<IActionResult> RemoveGroupMember(Guid groupId, Guid userId)
    {
        try
        {
            await _groupService.RemoveMemberAsync(groupId, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
