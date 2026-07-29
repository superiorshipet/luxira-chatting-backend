using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Application.Interfaces.Services;
using InternalChat.Domain.Entities;
using InternalChat.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using InternalChat.Infrastructure.Persistence;

namespace InternalChat.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtProvider _jwtProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICacheService _cacheService;
    private readonly AppDbContext _db;

    public UserService(IUnitOfWork unitOfWork, IJwtProvider jwtProvider, IPasswordHasher passwordHasher, ICacheService cacheService, AppDbContext db)
    {
        _unitOfWork     = unitOfWork;
        _jwtProvider    = jwtProvider;
        _passwordHasher = passwordHasher;
        _cacheService   = cacheService;
        _db             = db;
    }

    // ─────────────────── AUTH ───────────────────

    public async Task<LoginResponse?> LoginAsync(string phoneNumber, string password)
    {
        var user = await _unitOfWork.Users.GetByPhoneNumberAsync(phoneNumber);
        if (user == null || user.Status == UserStatus.Blocked) return null;
        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash)) return null;

        var token   = _jwtProvider.GenerateToken(user);
        var userDto = MapToDto(user);
        return new LoginResponse(token, userDto);
    }

    public async Task<string> ForgotPasswordAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) 
            return "If this email is registered, you will receive a reset link."; // Don't reveal existence

        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()); // Simple secure token
        user.PasswordResetToken          = token;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        // TODO: integrate real email sender (SMTP/SendGrid). For now, return token in response for dev testing.
        return $"Reset token (dev only): {token}";
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == email &&
            u.PasswordResetToken == token &&
            u.PasswordResetTokenExpiresAt > DateTime.UtcNow);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid or expired reset token.");

        user.PasswordHash               = _passwordHasher.HashPassword(newPassword);
        user.PasswordResetToken          = null;
        user.PasswordResetTokenExpiresAt = null;
        await _db.SaveChangesAsync();
    }

    // ─────────────────── ADMIN USER MANAGEMENT ───────────────────

    public async Task<UserDto> CreateUserAsync(string phoneNumber, string password, string fullName, string? email, Guid createdByAdminId)
    {
        if (await _unitOfWork.Users.GetByPhoneNumberAsync(phoneNumber) != null)
            throw new Exception("User with this phone number already exists.");

        if (email != null && await _db.Users.AnyAsync(u => u.Email == email))
            throw new Exception("User with this email already exists.");

        var user = new User
        {
            Id               = Guid.NewGuid(),
            PhoneNumber      = phoneNumber,
            Email            = email,
            PasswordHash     = _passwordHasher.HashPassword(password),
            FullName         = fullName,
            Role             = UserRole.Employee,
            Status           = UserStatus.Active,
            CreatedAt        = DateTime.UtcNow,
            CreatedByAdminId = createdByAdminId
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task BlockUserAsync(Guid userId, Guid adminId, string? reason)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId) ?? throw new Exception("User not found.");
        user.Status = UserStatus.Blocked;
        _unitOfWork.Users.Update(user);

        await _unitOfWork.UserBlocks.AddAsync(new UserBlock
        {
            Id               = Guid.NewGuid(),
            UserId           = userId,
            BlockedByAdminId = adminId,
            Reason           = reason,
            BlockedAt        = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
        await _cacheService.SetAsync($"user:status:{userId}", UserStatus.Blocked.ToString(), TimeSpan.FromMinutes(60));
    }

    public async Task UnblockUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId) ?? throw new Exception("User not found.");
        user.Status = UserStatus.Active;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        await _cacheService.SetAsync($"user:status:{userId}", UserStatus.Active.ToString(), TimeSpan.FromMinutes(60));
    }

    public async Task ToggleVerificationAsync(Guid userId, Guid adminId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId) ?? throw new Exception("User not found.");
        user.IsVerified = !user.IsVerified;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task GrantPrivateMessagePermissionAsync(Guid userId, Guid adminId, bool grant)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId) ?? throw new Exception("User not found.");
        user.CanReceivePrivateMessages = grant;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
        return users.Select(MapToDto);
    }

    // ─────────────────── PROFILES ───────────────────

    public async Task<UserPublicProfileDto?> GetPublicProfileAsync(Guid viewerId, Guid targetUserId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(targetUserId);
        if (user == null) return null;

        // Get media files sent by the target user in groups that the viewer is also in
        var sharedMedia = await _db.Attachments
            .Include(a => a.Message)
            .Where(a =>
                a.Message!.SenderId == targetUserId &&
                !a.Message.IsDeleted &&
                _db.GroupMembers.Any(gm => gm.GroupId == a.Message.GroupId && gm.UserId == viewerId && gm.RemovedAt == null))
            .OrderByDescending(a => a.Message!.SentAt)
            .Take(50)
            .Select(a => new SharedMediaDto(a.MessageId, a.FileUrl, a.FileType, a.Message!.SentAt))
            .ToListAsync();

        return new UserPublicProfileDto(
            user.Id,
            user.FullName,
            user.ProfileImageUrl,
            user.IsVerified,
            user.IsOnline,
            user.LastSeenAt,
            sharedMedia);
    }

    public async Task<UserDto?> GetProfileAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user == null ? null : MapToDto(user);
    }

    public async Task UpdateProfileAsync(Guid userId, string? fullName, string? profileImageUrl)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId) ?? throw new Exception("User not found.");
        if (fullName != null)         user.FullName         = fullName;
        if (profileImageUrl != null)  user.ProfileImageUrl  = profileImageUrl;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    // ─────────────────── PRIVATE CHATS ───────────────────

    public async Task<GroupDto> CreatePrivateChatAsync(Guid adminId, Guid targetUserId)
    {
        var targetUser = await _unitOfWork.Users.GetByIdAsync(targetUserId)
            ?? throw new Exception("Target user not found.");

        // Check no existing private chat between admin and this user
        var existing = await _db.Groups
            .FirstOrDefaultAsync(g => g.IsPrivate && g.CreatedByAdminId == adminId && g.PrivateTargetUserId == targetUserId);
        if (existing != null)
            throw new Exception("A private chat with this user already exists.");

        var group = new Group
        {
            Id                  = Guid.NewGuid(),
            Name                = $"Private: {targetUser.FullName}",
            CreatedByAdminId    = adminId,
            CreatedAt           = DateTime.UtcNow,
            IsPrivate           = true,
            PrivateTargetUserId = targetUserId
        };
        await _unitOfWork.Groups.AddAsync(group);

        // Add both admin and target as members; target starts as Muted (cannot reply by default)
        await _unitOfWork.GroupMembers.AddAsync(new GroupMember
        {
            Id              = Guid.NewGuid(),
            GroupId         = group.Id,
            UserId          = adminId,
            IsMuted         = false,
            JoinedAt        = DateTime.UtcNow,
            AddedByAdminId  = adminId
        });
        await _unitOfWork.GroupMembers.AddAsync(new GroupMember
        {
            Id              = Guid.NewGuid(),
            GroupId         = group.Id,
            UserId          = targetUserId,
            IsMuted         = true, // Cannot reply until admin grants permission
            JoinedAt        = DateTime.UtcNow,
            AddedByAdminId  = adminId
        });

        await _unitOfWork.SaveChangesAsync();
        return new GroupDto(group.Id, group.Name, group.ImageUrl, group.CreatedAt, IsPrivate: true, PrivateTargetUserId: targetUserId);
    }

    // ─────────────────── FAVOURITES ───────────────────

    public async Task ToggleFavoriteGroupAsync(Guid userId, Guid groupId)
    {
        var existing = await _db.UserFavoriteGroups
            .FirstOrDefaultAsync(f => f.UserId == userId && f.GroupId == groupId);

        if (existing != null)
            _db.UserFavoriteGroups.Remove(existing);
        else
            await _db.UserFavoriteGroups.AddAsync(new UserFavoriteGroup { UserId = userId, GroupId = groupId });

        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<GroupDto>> GetFavoriteGroupsAsync(Guid userId)
    {
        return await _db.UserFavoriteGroups
            .Where(f => f.UserId == userId)
            .Include(f => f.Group)
            .Select(f => new GroupDto(f.Group!.Id, f.Group.Name, f.Group.ImageUrl, f.Group.CreatedAt, f.Group.IsPrivate, f.Group.PrivateTargetUserId, IsFavorite: true))
            .ToListAsync();
    }

    // ─────────────────── HELPERS ───────────────────

    private static UserDto MapToDto(User u) => new(
        u.Id, u.PhoneNumber, u.FullName, u.ProfileImageUrl,
        u.Role, u.Status, u.IsOnline, u.LastSeenAt,
        u.IsVerified, u.CanReceivePrivateMessages);
}
