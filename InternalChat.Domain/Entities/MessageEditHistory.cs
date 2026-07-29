using InternalChat.Domain.Common;

namespace InternalChat.Domain.Entities;

/// <summary>
/// Tracks the edit history of a message.
/// </summary>
public class MessageEditHistory : BaseEntity
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }
    
    public required string OldContent { get; set; }
    public DateTime EditedAt { get; set; }
}
