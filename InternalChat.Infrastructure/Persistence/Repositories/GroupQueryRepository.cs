using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalChat.Infrastructure.Persistence.Repositories;

public class GroupQueryRepository : IGroupQueryRepository
{
    private readonly AppDbContext _db;
    public GroupQueryRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<GroupDto>> GetUserGroupsWithMetaAsync(Guid userId, string? filter)
    {
        var memberGroupIds = await _db.GroupMembers
            .Where(gm => gm.UserId == userId && gm.RemovedAt == null)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        var favoriteGroupIds = await _db.UserFavoriteGroups
            .Where(f => f.UserId == userId)
            .Select(f => f.GroupId)
            .ToListAsync();

        var readMessageIds = await _db.MessageReads
            .Where(mr => mr.UserId == userId)
            .Select(mr => mr.MessageId)
            .ToListAsync();

        var groups = await _db.Groups
            .Where(g => memberGroupIds.Contains(g.Id) && !g.IsArchived)
            .ToListAsync();

        var result = new List<GroupDto>();
        foreach (var g in groups)
        {
            var lastMsg = await _db.Messages
                .Where(m => m.GroupId == g.Id && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            var unreadCount = await _db.Messages
                .CountAsync(m => m.GroupId == g.Id && !m.IsDeleted
                    && !readMessageIds.Contains(m.Id)
                    && m.SenderId != userId);

            result.Add(new GroupDto(
                g.Id, g.Name, g.ImageUrl, g.CreatedAt,
                g.IsPrivate, g.PrivateTargetUserId,
                IsFavorite: favoriteGroupIds.Contains(g.Id),
                UnreadCount: unreadCount,
                LastMessage: lastMsg?.Content,
                LastMessageAt: lastMsg?.SentAt));
        }

        IEnumerable<GroupDto> filtered = filter switch
        {
            "unread"    => result.Where(g => g.UnreadCount > 0),
            "favorites" => result.Where(g => g.IsFavorite),
            _           => result
        };

        return filtered.OrderByDescending(g => g.LastMessageAt ?? g.CreatedAt);
    }

    public async Task<IEnumerable<GroupDto>> GetFavoriteGroupsAsync(Guid userId)
        => await _db.UserFavoriteGroups
            .Where(f => f.UserId == userId)
            .Include(f => f.Group)
            .Select(f => new GroupDto(f.Group!.Id, f.Group.Name, f.Group.ImageUrl,
                f.Group.CreatedAt, f.Group.IsPrivate, f.Group.PrivateTargetUserId, IsFavorite: true))
            .ToListAsync();

    public async Task<bool> FavoriteExistsAsync(Guid userId, Guid groupId)
        => await _db.UserFavoriteGroups.AnyAsync(f => f.UserId == userId && f.GroupId == groupId);

    public async Task AddFavoriteAsync(Guid userId, Guid groupId)
    {
        await _db.UserFavoriteGroups.AddAsync(new UserFavoriteGroup { UserId = userId, GroupId = groupId });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveFavoriteAsync(Guid userId, Guid groupId)
    {
        var fav = await _db.UserFavoriteGroups
            .FirstOrDefaultAsync(f => f.UserId == userId && f.GroupId == groupId);
        if (fav != null) { _db.UserFavoriteGroups.Remove(fav); await _db.SaveChangesAsync(); }
    }

    public async Task<IEnumerable<MessageDto>> SearchMessagesAsync(Guid userId, string keyword)
    {
        var memberGroupIds = await _db.GroupMembers
            .Where(gm => gm.UserId == userId && gm.RemovedAt == null)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        return await _db.Messages
            .Where(m =>
                memberGroupIds.Contains(m.GroupId) &&
                !m.IsDeleted &&
                m.Content != null &&
                EF.Functions.ILike(m.Content, $"%{keyword}%"))
            .Include(m => m.Sender)
            .OrderByDescending(m => m.SentAt)
            .Take(100)
            .Select(m => new MessageDto(
                m.Id,
                m.GroupId,
                m.SenderId,
                m.Sender!.FullName,
                m.Content,
                m.MessageType,
                m.SentAt,
                m.IsEdited,
                m.IsDeleted,
                m.IsPinned,
                m.ReplyToMessageId,
                m.ForwardedFromMessageId,
                m.ForwardedFromGroupId,
                new List<AttachmentDto>()))
            .ToListAsync();
    }

    public async Task<bool> PrivateChatExistsAsync(Guid adminId, Guid targetUserId)
        => await _db.Groups.AnyAsync(g =>
            g.IsPrivate &&
            g.CreatedByAdminId == adminId &&
            g.PrivateTargetUserId == targetUserId);
}
