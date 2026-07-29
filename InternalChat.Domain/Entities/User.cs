using InternalChat.Domain.Common;
using InternalChat.Domain.Enums;

namespace InternalChat.Domain.Entities;

/// <summary>
/// Represents a user in the internal chat system.
/// </summary>
public class User : BaseEntity
{
    public Guid Id { get; set; }
    public required string PhoneNumber { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByAdminId { get; set; }
    public User? CreatedByAdmin { get; set; }
}
