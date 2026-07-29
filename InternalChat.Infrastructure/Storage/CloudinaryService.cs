using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InternalChat.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace InternalChat.Infrastructure.Storage;

/// <summary>
/// Handles all file uploads to Cloudinary.
/// Replaces LocalFileStorage for production use.
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
        _cloudinary  = new Cloudinary(account) { Api = { Secure = true } };
    }

    /// <summary>Uploads a file stream to Cloudinary and returns its secure URL and metadata.</summary>
    public async Task<FileUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var folder    = DetermineFolder(contentType);
        var isImage   = contentType.StartsWith("image/");
        var isVideo   = contentType.StartsWith("video/");

        string secureUrl;
        long   bytes = fileStream.CanSeek ? fileStream.Length : 0;

        if (isImage)
        {
            var uploadParams = new ImageUploadParams
            {
                File           = new FileDescription(fileName, fileStream),
                Folder         = folder,
                UniqueFilename = true,
                Overwrite      = false,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
                throw new Exception($"Cloudinary image upload failed: {result.Error.Message}");
            secureUrl = result.SecureUrl.ToString();
            bytes     = result.Bytes;
        }
        else if (isVideo)
        {
            var uploadParams = new VideoUploadParams
            {
                File           = new FileDescription(fileName, fileStream),
                Folder         = folder,
                UniqueFilename = true,
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
                throw new Exception($"Cloudinary video upload failed: {result.Error.Message}");
            secureUrl = result.SecureUrl.ToString();
            bytes     = result.Bytes;
        }
        else
        {
            // Audio, PDFs, documents, etc.
            var uploadParams = new RawUploadParams
            {
                File           = new FileDescription(fileName, fileStream),
                Folder         = folder,
                UniqueFilename = true,
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
                throw new Exception($"Cloudinary raw upload failed: {result.Error.Message}");
            secureUrl = result.SecureUrl.ToString();
            bytes     = result.Bytes;
        }

        return new FileUploadResult(secureUrl, null, contentType, bytes);
    }

    private static string DetermineFolder(string contentType)
    {
        if (contentType.StartsWith("image/"))           return "chat/images";
        if (contentType.StartsWith("video/"))           return "chat/videos";
        if (contentType.StartsWith("audio/"))           return "chat/audio";
        if (contentType == "application/pdf")           return "chat/documents";
        return "chat/files";
    }
}
