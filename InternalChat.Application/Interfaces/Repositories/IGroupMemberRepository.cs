using InternalChat.Domain.Common;
using InternalChat.Domain.Entities;

namespace InternalChat.Application.Interfaces.Repositories;

/// <summary>
/// Repository for GroupMember entities.
/// </summary>
public interface IGroupMemberRepository : IRepository<GroupMember>
{
    Task<bool> IsActiveMemberAsync(Guid groupId, Guid userId);
    Task<IEnumerable<GroupMember>> GetActiveMembersAsync(Guid groupId);
    Task<IEnumerable<Guid>> GetGroupIdsForUserAsync(Guid userId);
}
