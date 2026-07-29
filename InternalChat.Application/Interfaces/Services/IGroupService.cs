using InternalChat.Application.DTOs;

namespace InternalChat.Application.Interfaces.Services;

public interface IGroupService
{
    Task<GroupDto> CreateGroupAsync(string name, string? imageUrl, Guid adminId);
    Task AddMembersAsync(Guid groupId, IEnumerable<Guid> userIds, Guid adminId);
    Task RemoveMemberAsync(Guid groupId, Guid userId);
    Task MuteMemberAsync(Guid groupId, Guid userId, bool isMuted);
    
    Task<IEnumerable<GroupDto>> GetUserGroupsAsync(Guid userId);
    Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(Guid groupId, Guid callerUserId);
}
