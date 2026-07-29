using InternalChat.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternalChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;

    public UsersController(IUserService userService, IGroupService groupService)
    {
        _userService  = userService;
        _groupService = groupService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Get the public profile of any user.
    /// Phone number is NEVER returned here.
    /// Includes shared media files sent in common groups.
    /// </summary>
    [HttpGet("{userId}/profile")]
    public async Task<IActionResult> GetPublicProfile(Guid userId)
    {
        var profile = await _userService.GetPublicProfileAsync(CurrentUserId, userId);
        return profile == null ? NotFound() : Ok(profile);
    }

    /// <summary>
    /// Toggle a group as favourite/unfavourite for the current user.
    /// </summary>
    [HttpPost("favorites/{groupId}")]
    public async Task<IActionResult> ToggleFavorite(Guid groupId)
    {
        await _userService.ToggleFavoriteGroupAsync(CurrentUserId, groupId);
        return Ok(new { message = "Favourite toggled." });
    }
}
