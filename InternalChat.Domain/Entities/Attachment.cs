using InternalChat.Domain.Common;

namespace InternalChat.Domain.Entities;

/// <summary>
/// Represents a file attachment for a message.
/// </summary>
public class Attachment : BaseEntity
{
    public Guid Id { get; set; }
    
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }
    
    public required string FileUrl { get; set; }
    public required string FileType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? DurationSeconds { get; set; }
}
