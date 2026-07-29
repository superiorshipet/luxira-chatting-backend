using InternalChat.Domain.Enums;

namespace InternalChat.Application.DTOs;

// Auth
public record LoginRequest(string PhoneNumber, string Password);
public record LoginResponse(string Token, UserDto User);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);

// User DTOs
public record UserDto(
    Guid Id,
    string PhoneNumber,
    string FullName,
    string? ProfileImageUrl,
    UserRole Role,
    UserStatus Status,
    bool IsOnline,
    DateTime? LastSeenAt,
    bool IsVerified = false,
    bool CanReceivePrivateMessages = false);

/// <summary>
/// Public profile — NEVER exposes PhoneNumber.
/// </summary>
public record UserPublicProfileDto(
    Guid Id,
    string FullName,
    string? ProfileImageUrl,
    bool IsVerified,
    bool IsOnline,
    DateTime? LastSeenAt,
    IEnumerable<SharedMediaDto> SharedMedia);

public record SharedMediaDto(Guid MessageId, string Url, string FileType, DateTime SentAt);

// Admin Requests
public record CreateUserRequest(string PhoneNumber, string Password, string FullName, string? Email);
public record UpdateProfileRequest(string? FullName, string? ProfileImageUrl);
public record GrantPrivatePermissionRequest(bool Grant);
