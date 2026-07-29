using InternalChat.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalChat.API.Controllers;

/// <summary>
/// Handles file and image uploads via Cloudinary.
/// Returns Cloudinary CDN URLs for use in messages and profiles.
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
    /// Returns the public CDN URL. Max size: 50MB.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _fileStorage.UploadAsync(stream, file.FileName, file.ContentType);
            return Ok(new { url = result.Url, fileName = file.FileName, size = result.SizeBytes, fileType = result.FileType });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Upload a profile picture (images only, max 10MB).
    /// Caller should then call PUT /api/Auth/profile with the returned URL.
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
            var result = await _fileStorage.UploadAsync(stream, file.FileName, file.ContentType);
            return Ok(new { url = result.Url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
        }
    }
}
