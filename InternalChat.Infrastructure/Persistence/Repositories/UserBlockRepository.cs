using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;

namespace InternalChat.Infrastructure.Persistence.Repositories;

/// <summary>
/// Concrete repository for UserBlock entity.
/// </summary>
public class UserBlockRepository : EfRepository<UserBlock>, IUserBlockRepository
{
    public UserBlockRepository(AppDbContext context) : base(context)
    {
    }
}
