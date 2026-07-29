using InternalChat.Domain.Common;

namespace InternalChat.Domain.Entities;

/// <summary>
/// Represents a reaction to a message.
/// </summary>
public class MessageReaction : BaseEntity
{
    public Guid Id { get; set; }
    
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public required string Emoji { get; set; }
    public DateTime ReactedAt { get; set; }
}
