using InternalChat.Application.DTOs;
using InternalChat.Domain.Entities;

namespace InternalChat.Application.Interfaces.Services;

/// <summary>
/// Orchestrates business logic related to Users, including Authentication and Admin operations.
/// </summary>
public interface IUserService
{
    // Auth
    Task<LoginResponse?> LoginAsync(string phoneNumber, string password);
    Task<string> ForgotPasswordAsync(string phoneNumber, string email);
    Task ResetPasswordAsync(string email, string token, string newPassword);

    // Admin user management
    Task<UserDto> CreateUserAsync(string phoneNumber, string password, string fullName, string? email, Guid createdByAdminId);
    Task BlockUserAsync(Guid userId, Guid adminId, string? reason);
    Task UnblockUserAsync(Guid userId);
    Task ToggleVerificationAsync(Guid userId, Guid adminId);
    Task GrantPrivateMessagePermissionAsync(Guid userId, Guid adminId, bool grant);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    // User profile
    Task<UserPublicProfileDto?> GetPublicProfileAsync(Guid viewerId, Guid targetUserId);
    Task<UserDto?> GetProfileAsync(Guid userId);
    Task UpdateProfileAsync(Guid userId, string? fullName, string? profileImageUrl);

    // Private messaging (Admin-only)
    Task<GroupDto> CreatePrivateChatAsync(Guid adminId, Guid targetUserId);

    // Favourites
    Task ToggleFavoriteGroupAsync(Guid userId, Guid groupId);
    Task<IEnumerable<GroupDto>> GetFavoriteGroupsAsync(Guid userId);
}
