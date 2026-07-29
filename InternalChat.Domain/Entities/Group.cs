using InternalChat.Domain.Common;

namespace InternalChat.Domain.Entities;

/// <summary>
/// Represents a chat group managed by admins.
/// </summary>
public class Group : BaseEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CreatedByAdminId { get; set; }
    public User? CreatedByAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsArchived { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
