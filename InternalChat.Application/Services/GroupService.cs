using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Application.Interfaces.Services;
using InternalChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using InternalChat.Infrastructure.Persistence;

namespace InternalChat.Application.Services;

public class GroupService : IGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly AppDbContext _db;

    public GroupService(IUnitOfWork unitOfWork, ICacheService cacheService, AppDbContext db)
    {
        _unitOfWork   = unitOfWork;
        _cacheService = cacheService;
        _db           = db;
    }

    public async Task<GroupDto> CreateGroupAsync(string name, string? imageUrl, Guid adminId)
    {
        var group = new Group
        {
            Id               = Guid.NewGuid(),
            Name             = name,
            ImageUrl         = imageUrl,
            CreatedByAdminId = adminId,
            CreatedAt        = DateTime.UtcNow,
            IsArchived       = false,
            IsPrivate        = false
        };

        await _unitOfWork.Groups.AddAsync(group);
        await _unitOfWork.SaveChangesAsync();
        return new GroupDto(group.Id, group.Name, group.ImageUrl, group.CreatedAt);
    }

    public async Task AddMembersAsync(Guid groupId, IEnumerable<Guid> userIds, Guid adminId)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(groupId) ?? throw new Exception("Group not found");

        foreach (var userId in userIds)
        {
            if (!await _unitOfWork.GroupMembers.IsActiveMemberAsync(groupId, userId))
            {
                await _unitOfWork.GroupMembers.AddAsync(new GroupMember
                {
                    Id             = Guid.NewGuid(),
                    GroupId        = groupId,
                    UserId         = userId,
                    IsMuted        = false,
                    JoinedAt       = DateTime.UtcNow,
                    AddedByAdminId = adminId
                });
                await _cacheService.RemoveAsync($"member:{groupId}:{userId}");
                await _cacheService.RemoveAsync($"user:groups:{userId}");
            }
        }

        await _unitOfWork.SaveChangesAsync();
        await _cacheService.RemoveAsync($"group:members:{groupId}");
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid userId)
    {
        var members = await _unitOfWork.GroupMembers.FindAsync(gm =>
            gm.GroupId == groupId && gm.UserId == userId && gm.RemovedAt == null);
        var member = members.SingleOrDefault();
        if (member != null)
        {
            member.RemovedAt = DateTime.UtcNow;
            _unitOfWork.GroupMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();
            await _cacheService.RemoveAsync($"member:{groupId}:{userId}");
            await _cacheService.RemoveAsync($"user:groups:{userId}");
            await _cacheService.RemoveAsync($"group:members:{groupId}");
        }
    }

    public async Task MuteMemberAsync(Guid groupId, Guid userId, bool isMuted)
    {
        var members = await _unitOfWork.GroupMembers.FindAsync(gm =>
            gm.GroupId == groupId && gm.UserId == userId && gm.RemovedAt == null);
        var member = members.SingleOrDefault();
        if (member != null)
        {
            member.IsMuted = isMuted;
            _unitOfWork.GroupMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();
            await _cacheService.RemoveAsync($"member:{groupId}:{userId}");
        }
    }

    public async Task<IEnumerable<GroupDto>> GetUserGroupsAsync(Guid userId)
        => await GetUserGroupsFilteredAsync(userId, null);

    public async Task<IEnumerable<GroupDto>> GetUserGroupsFilteredAsync(Guid userId, string? filter)
    {
        // Get all active group memberships for this user
        var memberGroupIds = await _db.GroupMembers
            .Where(gm => gm.UserId == userId && gm.RemovedAt == null)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        var favoriteGroupIds = await _db.UserFavoriteGroups
            .Where(f => f.UserId == userId)
            .Select(f => f.GroupId)
            .ToListAsync();

        // Get unread counts for each group
        var readMessageIds = await _db.MessageReads
            .Where(mr => mr.UserId == userId)
            .Select(mr => mr.MessageId)
            .ToListAsync();

        var groups = await _db.Groups
            .Where(g => memberGroupIds.Contains(g.Id) && !g.IsArchived)
            .Include(g => g.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .ToListAsync();

        var result = groups.Select(g =>
        {
            var isFavorite   = favoriteGroupIds.Contains(g.Id);
            var lastMsg      = g.Messages.FirstOrDefault();
            var unreadCount  = _db.Messages
                .Count(m => m.GroupId == g.Id && !m.IsDeleted && !readMessageIds.Contains(m.Id) && m.SenderId != userId);

            return new GroupDto(
                g.Id, g.Name, g.ImageUrl, g.CreatedAt,
                g.IsPrivate, g.PrivateTargetUserId,
                IsFavorite: isFavorite,
                UnreadCount: unreadCount,
                LastMessage: lastMsg?.Content,
                LastMessageAt: lastMsg?.SentAt);
        }).AsEnumerable();

        // Apply filter
        result = filter switch
        {
            "unread"    => result.Where(g => g.UnreadCount > 0),
            "favorites" => result.Where(g => g.IsFavorite),
            _           => result
        };

        return result.OrderByDescending(g => g.LastMessageAt ?? g.CreatedAt);
    }

    public async Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(Guid groupId, Guid callerUserId)
    {
        if (!await _unitOfWork.GroupMembers.IsActiveMemberAsync(groupId, callerUserId))
            throw new UnauthorizedAccessException("You are not a member of this group.");

        var members = await _unitOfWork.GroupMembers.GetActiveMembersAsync(groupId);
        return members.Select(m => new GroupMemberDto(
            m.User!.Id,
            m.User.FullName,
            m.User.ProfileImageUrl,
            m.User.Role.ToString(),
            m.User.IsOnline,
            m.User.LastSeenAt,
            m.IsMuted,
            m.User.IsVerified));
    }

    /// <summary>Full-text search across all messages in groups the user belongs to.</summary>
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
                m.Sender.ProfileImageUrl,
                m.Content,
                m.MessageType.ToString(),
                m.SentAt,
                m.IsEdited,
                m.IsDeleted,
                m.IsPinned,
                m.ReplyToMessageId,
                null, null, null))
            .ToListAsync();
    }
}
