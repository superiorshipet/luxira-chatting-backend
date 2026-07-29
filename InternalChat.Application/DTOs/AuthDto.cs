using InternalChat.Domain.Enums;

namespace InternalChat.Application.DTOs;

public record LoginRequest(string PhoneNumber, string Password);
public record LoginResponse(string Token, UserDto User);

public record UserDto(
    Guid Id, 
    string PhoneNumber, 
    string FullName, 
    string? ProfileImageUrl, 
    UserRole Role, 
    UserStatus Status, 
    bool IsOnline, 
    DateTime? LastSeenAt);

public record CreateUserRequest(string PhoneNumber, string Password, string FullName);
