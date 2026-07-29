using InternalChat.Application.DTOs;
using InternalChat.Domain.Entities;

namespace InternalChat.Application.Interfaces.Repositories;

/// <summary>
/// Complex read queries for Users that go beyond simple CRUD.
/// Lives in Application, implemented in Infrastructure using EF Core.
/// </summary>
public interface IUserQueryRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<IEnumerable<SharedMediaDto>> GetSharedMediaAsync(Guid senderId, Guid viewerUserId);
}
