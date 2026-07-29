using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalChat.Infrastructure.Persistence.Repositories;

public class UserQueryRepository : IUserQueryRepository
{
    private readonly AppDbContext _db;
    public UserQueryRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<bool> EmailExistsAsync(string email)
        => await _db.Users.AnyAsync(u => u.Email == email);

    public async Task<IEnumerable<User>> GetAllUsersAsync()
        => await _db.Users.OrderBy(u => u.FullName).ToListAsync();

    public async Task<IEnumerable<SharedMediaDto>> GetSharedMediaAsync(Guid senderId, Guid viewerUserId)
    {
        return await _db.Attachments
            .Include(a => a.Message)
            .Where(a =>
                a.Message!.SenderId == senderId &&
                !a.Message.IsDeleted &&
                _db.GroupMembers.Any(gm =>
                    gm.GroupId == a.Message.GroupId &&
                    gm.UserId == viewerUserId &&
                    gm.RemovedAt == null))
            .OrderByDescending(a => a.Message!.SentAt)
            .Take(50)
            .Select(a => new SharedMediaDto(a.MessageId, a.FileUrl, a.FileType, a.Message!.SentAt))
            .ToListAsync();
    }
}
