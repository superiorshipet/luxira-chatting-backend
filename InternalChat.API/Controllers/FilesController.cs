using InternalChat.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternalChat.API.Controllers;

/// <summary>
/// Handles file and image uploads via Cloudinary.
/// All responses include the Cloudinary CDN URL for use in messages and profiles.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileStorage _fileStorage;

    public FilesController(IFileStorage fileStorage)
        => _fileStorage = fileStorage;

    /// <summary>
    /// Upload any file (image, video, audio, document) to Cloudinary.
    /// Returns the public CDN URL.
    /// Max size: 50MB.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder = "chat")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        try
        {
            await using var stream = file.OpenReadStream();
            var url = await _fileStorage.SaveFileAsync(stream, file.FileName, folder);
            return Ok(new { url, fileName = file.FileName, size = file.Length });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Upload a profile picture specifically (goes to "profiles" folder in Cloudinary).
    /// </summary>
    [HttpPost("upload/profile")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { error = "Only image files are allowed for profile pictures." });

        try
        {
            await using var stream = file.OpenReadStream();
            var url = await _fileStorage.SaveFileAsync(stream, file.FileName, "profiles");
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
        }
    }
}
