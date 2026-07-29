namespace InternalChat.Domain.Entities;

/// <summary>
/// Represents a read receipt for a message.
/// </summary>
public class MessageRead
{
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public DateTime ReadAt { get; set; }
}
