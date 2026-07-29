using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Application.Interfaces.Services;
using InternalChat.Domain.Entities;

namespace InternalChat.Application.Services;

public class GroupService : IGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IGroupQueryRepository _groupQuery;

    public GroupService(IUnitOfWork unitOfWork, ICacheService cacheService, IGroupQueryRepository groupQuery)
    {
        _unitOfWork  = unitOfWork;
        _cacheService = cacheService;
        _groupQuery   = groupQuery;
    }

    public async Task<GroupDto> CreateGroupAsync(string name, string? imageUrl, Guid adminId)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(), Name = name, ImageUrl = imageUrl,
            CreatedByAdminId = adminId, CreatedAt = DateTime.UtcNow,
            IsArchived = false, IsPrivate = false
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
                    Id = Guid.NewGuid(), GroupId = groupId, UserId = userId,
                    IsMuted = false, JoinedAt = DateTime.UtcNow, AddedByAdminId = adminId
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
        => await _groupQuery.GetUserGroupsWithMetaAsync(userId, filter);

    public async Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(Guid groupId, Guid callerUserId)
    {
        if (!await _unitOfWork.GroupMembers.IsActiveMemberAsync(groupId, callerUserId))
            throw new UnauthorizedAccessException("You are not a member of this group.");

        var members = await _unitOfWork.GroupMembers.GetActiveMembersAsync(groupId);
        return members.Select(m => new GroupMemberDto(
            m.User!.Id, m.User.FullName, m.User.ProfileImageUrl,
            m.User.Role.ToString(), m.User.IsOnline, m.User.LastSeenAt,
            m.IsMuted, m.User.IsVerified));
    }

    public async Task<IEnumerable<MessageDto>> SearchMessagesAsync(Guid userId, string keyword)
        => await _groupQuery.SearchMessagesAsync(userId, keyword);
}
