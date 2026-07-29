namespace InternalChat.Domain.Entities;

/// <summary>
/// Stores a camera-captured photo taken directly within the app.
/// </summary>
public class CapturedMedia
{
    public Guid Id { get; set; }
    public Guid UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
    public required string CloudinaryUrl { get; set; }
    public required string CloudinaryPublicId { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    
    // Optional: linked to a message if sent in chat
    public Guid? LinkedMessageId { get; set; }
    public Message? LinkedMessage { get; set; }
}
