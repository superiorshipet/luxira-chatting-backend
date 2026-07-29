using InternalChat.Application.Interfaces.Services;

namespace InternalChat.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _uploadDirectory;
    private readonly string _baseUrl;

    public LocalFileStorage()
    {
        _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        _baseUrl = "/uploads/";
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public async Task<FileUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }
        
        var sizeBytes = new FileInfo(filePath).Length;

        return new FileUploadResult($"{_baseUrl}{uniqueFileName}", null, contentType, sizeBytes);
    }
}
