using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternalChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService) => _userService = userService;

    /// <summary>Login with phone number and password.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.LoginAsync(request.PhoneNumber, request.Password);
        if (result == null) return Unauthorized("Invalid credentials or user is blocked.");
        return Ok(result);
    }

    /// <summary>Request a password-reset token (sent to email).</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var message = await _userService.ForgotPasswordAsync(request.PhoneNumber, request.Email);
        return Ok(new { message });
    }

    /// <summary>Reset password using the token received by email.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _userService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
            return Ok(new { message = "Password reset successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get the profile of the currently authenticated user.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _userService.GetProfileAsync(userId);
        return profile == null ? NotFound() : Ok(profile);
    }

    /// <summary>Update the authenticated user's own profile (name or profile image).</summary>
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.UpdateProfileAsync(userId, request.FullName, request.ProfileImageUrl);
        return Ok(new { message = "Profile updated." });
    }
}
