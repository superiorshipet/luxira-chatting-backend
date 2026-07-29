using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;

namespace InternalChat.Infrastructure.Persistence.Repositories;

/// <summary>
/// Concrete repository for Group entity.
/// </summary>
public class GroupRepository : EfRepository<Group>, IGroupRepository
{
    public GroupRepository(AppDbContext context) : base(context)
    {
    }
}
