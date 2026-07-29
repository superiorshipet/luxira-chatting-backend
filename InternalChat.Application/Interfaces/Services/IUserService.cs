using InternalChat.Application.DTOs;
using InternalChat.Domain.Entities;

namespace InternalChat.Application.Interfaces.Services;

/// <summary>
/// Orchestrates business logic related to Users, including Authentication and Admin operations.
/// </summary>
public interface IUserService
{
    Task<LoginResponse?> LoginAsync(string phoneNumber, string password);
    Task<UserDto> CreateUserAsync(string phoneNumber, string password, string fullName, Guid createdByAdminId);
    Task BlockUserAsync(Guid userId, Guid adminId, string? reason);
    Task UnblockUserAsync(Guid userId);
    Task<UserDto?> GetProfileAsync(Guid userId);
}
