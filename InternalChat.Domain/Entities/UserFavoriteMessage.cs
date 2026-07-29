using System;

namespace InternalChat.Domain.Entities;

/// <summary>
/// Tracks which individual messages a user has marked as favorite.
/// </summary>
public class UserFavoriteMessage
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public DateTime FavoritedAt { get; set; } = DateTime.UtcNow;
}
