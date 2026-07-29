namespace InternalChat.Application.Interfaces.Services;

public record FileUploadResult(string Url, string? ThumbnailUrl, string FileType, long SizeBytes);

/// <summary>
/// Abstraction for storing uploaded attachments.
/// </summary>
public interface IFileStorage
{
    Task<FileUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType);
}
