using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Application.Interfaces.Services;
using InternalChat.Domain.Entities;
using InternalChat.Domain.Enums;

namespace InternalChat.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtProvider _jwtProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICacheService _cacheService;

    public UserService(IUnitOfWork unitOfWork, IJwtProvider jwtProvider, IPasswordHasher passwordHasher, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _jwtProvider = jwtProvider;
        _passwordHasher = passwordHasher;
        _cacheService = cacheService;
    }

    public async Task<LoginResponse?> LoginAsync(string phoneNumber, string password)
    {
        var user = await _unitOfWork.Users.GetByPhoneNumberAsync(phoneNumber);
        if (user == null || user.Status == UserStatus.Blocked)
            return null;

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            return null;

        var token = _jwtProvider.GenerateToken(user);
        
        var userDto = new UserDto(
            user.Id,
            user.PhoneNumber,
            user.FullName,
            user.ProfileImageUrl,
            user.Role,
            user.Status,
            user.IsOnline,
            user.LastSeenAt
        );

        return new LoginResponse(token, userDto);
    }

    public async Task<UserDto> CreateUserAsync(string phoneNumber, string password, string fullName, Guid createdByAdminId)
    {
        var existingUser = await _unitOfWork.Users.GetByPhoneNumberAsync(phoneNumber);
        if (existingUser != null)
            throw new Exception("User with this phone number already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            PasswordHash = _passwordHasher.HashPassword(password),
            FullName = fullName,
            Role = UserRole.Employee,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedByAdminId = createdByAdminId
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new UserDto(user.Id, user.PhoneNumber, user.FullName, user.ProfileImageUrl, user.Role, user.Status, user.IsOnline, user.LastSeenAt);
    }

    public async Task BlockUserAsync(Guid userId, Guid adminId, string? reason)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        user.Status = UserStatus.Blocked;
        _unitOfWork.Users.Update(user);

        var blockRecord = new UserBlock
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BlockedByAdminId = adminId,
            Reason = reason,
            BlockedAt = DateTime.UtcNow
        };
        await _unitOfWork.UserBlocks.AddAsync(blockRecord);

        await _unitOfWork.SaveChangesAsync();

        var cacheKey = $"user:status:{userId}";
        await _cacheService.SetAsync(cacheKey, UserStatus.Blocked.ToString(), TimeSpan.FromMinutes(60));
    }

    public async Task UnblockUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found.");

        user.Status = UserStatus.Active;
        _unitOfWork.Users.Update(user);

        await _unitOfWork.SaveChangesAsync();
        
        var cacheKey = $"user:status:{userId}";
        await _cacheService.SetAsync(cacheKey, UserStatus.Active.ToString(), TimeSpan.FromMinutes(60));
    }

    public async Task<UserDto?> GetProfileAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return null;

        return new UserDto(user.Id, user.PhoneNumber, user.FullName, user.ProfileImageUrl, user.Role, user.Status, user.IsOnline, user.LastSeenAt);
    }
}
