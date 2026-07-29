using InternalChat.Domain.Common;

namespace InternalChat.Domain.Entities;

/// <summary>
/// Represents a block record for a user.
/// </summary>
public class UserBlock : BaseEntity
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public Guid BlockedByAdminId { get; set; }
    public User? BlockedByAdmin { get; set; }
    
    public string? Reason { get; set; }
    public DateTime BlockedAt { get; set; }
    public DateTime? UnblockedAt { get; set; }
}
