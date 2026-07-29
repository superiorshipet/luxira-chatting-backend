using InternalChat.Domain.Common;

namespace InternalChat.Domain.Entities;

/// <summary>
/// Represents a user's membership in a group.
/// </summary>
public class GroupMember : BaseEntity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public bool IsMuted { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
    
    public Guid AddedByAdminId { get; set; }
    public User? AddedByAdmin { get; set; }
}
