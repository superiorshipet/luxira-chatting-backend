using InternalChat.Domain.Common;
using InternalChat.Domain.Enums;

namespace InternalChat.Domain.Entities;

/// <summary>
/// Represents a message sent within a group.
/// </summary>
public class Message : BaseEntity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }
    
    public Guid SenderId { get; set; }
    public User? Sender { get; set; }
    
    public string? Content { get; set; }
    public MessageType MessageType { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsPinned { get; set; }
    
    public Guid? ReplyToMessageId { get; set; }
    public Message? ReplyToMessage { get; set; }
    
    public Guid? ForwardedFromMessageId { get; set; }
    public Message? ForwardedFromMessage { get; set; }
    
    public Guid? ForwardedFromGroupId { get; set; }
    
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    public ICollection<MessageRead> Reads { get; set; } = new List<MessageRead>();
    public ICollection<MessageEditHistory> EditHistories { get; set; } = new List<MessageEditHistory>();
}
