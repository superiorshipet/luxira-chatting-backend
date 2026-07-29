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
    
    /// <summary>True when this is a 1-on-1 private conversation initiated by admin.</summary>
    public bool IsPrivate { get; set; }
    
    /// <summary>For private groups: the non-admin participant (admin can speak, this user needs CanReceivePrivateMessages).</summary>
    public Guid? PrivateTargetUserId { get; set; }
    public User? PrivateTargetUser { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<UserFavoriteGroup> FavoritedBy { get; set; } = new List<UserFavoriteGroup>();
}
