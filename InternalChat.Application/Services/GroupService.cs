using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Application.Interfaces.Services;
using InternalChat.Domain.Entities;

namespace InternalChat.Application.Services;

public class GroupService : IGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public GroupService(IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<GroupDto> CreateGroupAsync(string name, string? imageUrl, Guid adminId)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            ImageUrl = imageUrl,
            CreatedByAdminId = adminId,
            CreatedAt = DateTime.UtcNow,
            IsArchived = false
        };

        await _unitOfWork.Groups.AddAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return new GroupDto(group.Id, group.Name, group.ImageUrl, group.CreatedAt);
    }

    public async Task AddMembersAsync(Guid groupId, IEnumerable<Guid> userIds, Guid adminId)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null) throw new Exception("Group not found");

        foreach (var userId in userIds)
        {
            var isActive = await _unitOfWork.GroupMembers.IsActiveMemberAsync(groupId, userId);
            if (!isActive)
            {
                var member = new GroupMember
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = userId,
                    IsMuted = false,
                    JoinedAt = DateTime.UtcNow,
                    AddedByAdminId = adminId
                };
                await _unitOfWork.GroupMembers.AddAsync(member);
                
                await _cacheService.RemoveAsync($"member:{groupId}:{userId}");
                await _cacheService.RemoveAsync($"user:groups:{userId}");
            }
        }

        await _unitOfWork.SaveChangesAsync();
        await _cacheService.RemoveAsync($"group:members:{groupId}");
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid userId)
    {
        var members = await _unitOfWork.GroupMembers.FindAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.RemovedAt == null);
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
        var members = await _unitOfWork.GroupMembers.FindAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.RemovedAt == null);
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
    {
        var groupIds = await _unitOfWork.GroupMembers.GetGroupIdsForUserAsync(userId);
        
        var groups = new List<GroupDto>();
        foreach (var id in groupIds)
        {
            var g = await _unitOfWork.Groups.GetByIdAsync(id);
            if (g != null && !g.IsArchived)
            {
                groups.Add(new GroupDto(g.Id, g.Name, g.ImageUrl, g.CreatedAt));
            }
        }
        
        return groups.OrderByDescending(g => g.CreatedAt);
    }

    public async Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(Guid groupId, Guid callerUserId)
    {
        var isActive = await _unitOfWork.GroupMembers.IsActiveMemberAsync(groupId, callerUserId);
        if (!isActive) throw new UnauthorizedAccessException("You are not a member of this group.");
        
        var members = await _unitOfWork.GroupMembers.GetActiveMembersAsync(groupId);
        
        return members.Select(m => new GroupMemberDto(
            m.User!.Id,
            m.User.FullName,
            m.User.ProfileImageUrl,
            m.User.Role.ToString(),
            m.User.IsOnline,
            m.User.LastSeenAt,
            m.IsMuted
        ));
    }
}
