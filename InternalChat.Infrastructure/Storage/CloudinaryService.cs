using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InternalChat.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace InternalChat.Infrastructure.Storage;

/// <summary>
/// Handles all file uploads to Cloudinary.
/// Replaces the LocalFileStorage implementation for production use.
/// </summary>
public class CloudinaryService : IFileStorage
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"]!;
        var apiKey    = configuration["Cloudinary:ApiKey"]!;
        var apiSecret = configuration["Cloudinary:ApiSecret"]!;

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    /// <summary>Uploads a file stream to Cloudinary and returns its secure URL.</summary>
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder = "chat")
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLower();
        var isImage   = new[] { "jpg", "jpeg", "png", "gif", "webp", "bmp" }.Contains(extension);
        var isVideo   = new[] { "mp4", "mov", "avi", "webm" }.Contains(extension);
        var isAudio   = new[] { "mp3", "wav", "ogg", "m4a", "webm" }.Contains(extension);

        if (isImage)
        {
            var uploadParams = new ImageUploadParams
            {
                File          = new FileDescription(fileName, fileStream),
                Folder        = folder,
                UseFilename   = true,
                UniqueFilename = true,
                Overwrite     = false,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");
            return result.SecureUrl.ToString();
        }
        else if (isVideo)
        {
            var uploadParams = new VideoUploadParams
            {
                File        = new FileDescription(fileName, fileStream),
                Folder      = folder,
                UseFilename = true,
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");
            return result.SecureUrl.ToString();
        }
        else
        {
            // Raw file (audio, document, etc.)
            var uploadParams = new RawUploadParams
            {
                File        = new FileDescription(fileName, fileStream),
                Folder      = folder,
                UseFilename = true,
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");
            return result.SecureUrl.ToString();
        }
    }

    /// <summary>Deletes a file from Cloudinary by its public ID.</summary>
    public async Task DeleteFileAsync(string fileUrl)
    {
        // Extract publicId from the URL (e.g., "chat/filename" from full URL)
        try
        {
            var uri      = new Uri(fileUrl);
            var segments = uri.AbsolutePath.Split('/');
            // Find "upload" and take everything after it, minus the extension
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex >= 0 && uploadIndex < segments.Length - 1)
            {
                var publicIdWithExt = string.Join("/", segments.Skip(uploadIndex + 1));
                var publicId        = Path.ChangeExtension(publicIdWithExt, null);
                await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            }
        }
        catch
        {
            // Best-effort deletion, don't throw
        }
    }
}
