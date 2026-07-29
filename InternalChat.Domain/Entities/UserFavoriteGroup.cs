namespace InternalChat.Domain.Entities;

/// <summary>
/// Tracks which groups a user has marked as favourite (for sidebar filtering).
/// </summary>
public class UserFavoriteGroup
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    public DateTime FavoritedAt { get; set; } = DateTime.UtcNow;
}
