using InternalChat.Domain.Common;
using InternalChat.Domain.Entities;
using InternalChat.Domain.Enums;

namespace InternalChat.Application.Interfaces.Repositories;

/// <summary>
/// Repository for User entities.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<UserStatus?> GetStatusAsync(Guid userId);
}
