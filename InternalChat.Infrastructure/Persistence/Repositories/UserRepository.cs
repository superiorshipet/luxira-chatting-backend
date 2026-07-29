using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;
using InternalChat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InternalChat.Infrastructure.Persistence.Repositories;

/// <summary>
/// Concrete repository for User entity.
/// </summary>
public class UserRepository : EfRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Users.SingleOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<UserStatus?> GetStatusAsync(Guid userId)
    {
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Status })
            .SingleOrDefaultAsync();

        return user?.Status;
    }
}
