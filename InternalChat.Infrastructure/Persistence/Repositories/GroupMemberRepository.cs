using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalChat.Infrastructure.Persistence.Repositories;

/// <summary>
/// Concrete repository for GroupMember entity.
/// </summary>
public class GroupMemberRepository : EfRepository<GroupMember>, IGroupMemberRepository
{
    public GroupMemberRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> IsActiveMemberAsync(Guid groupId, Guid userId)
    {
        return await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.RemovedAt == null);
    }

    public async Task<IEnumerable<GroupMember>> GetActiveMembersAsync(Guid groupId)
    {
        return await _context.GroupMembers
            .Include(gm => gm.User)
            .Where(gm => gm.GroupId == groupId && gm.RemovedAt == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Guid>> GetGroupIdsForUserAsync(Guid userId)
    {
        return await _context.GroupMembers
            .Where(gm => gm.UserId == userId && gm.RemovedAt == null)
            .Select(gm => gm.GroupId)
            .ToListAsync();
    }
}
