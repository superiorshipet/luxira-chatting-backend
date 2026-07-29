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
    private readonly IUserQueryRepository _userQuery;
    private readonly IGroupQueryRepository _groupQuery;
    private readonly IEmailService _emailService;

    public UserService(
        IUnitOfWork unitOfWork,
        IJwtProvider jwtProvider,
        IPasswordHasher passwordHasher,
        ICacheService cacheService,
        IUserQueryRepository userQuery,
        IGroupQueryRepository groupQuery,
        IEmailService emailService)
    {
        _unitOfWork     = unitOfWork;
        _jwtProvider    = jwtProvider;
        _passwordHasher = passwordHasher;
        _cacheService   = cacheService;
        _userQuery      = userQuery;
        _groupQuery     = groupQuery;
        _emailService   = emailService;
    }

    // ─────────────────── AUTH ───────────────────

    public async Task<LoginResponse?> LoginAsync(string phoneNumber, string password)
    {
        var user = await _unitOfWork.Users.GetByPhoneNumberAsync(phoneNumber);
        if (user == null || user.Status == UserStatus.Blocked) return null;
        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash)) return null;

        var token = _jwtProvider.GenerateToken(user);
        return new LoginResponse(token, MapToDto(user));
    }

    public async Task<string> ForgotPasswordAsync(string phoneNumber, string email)
    {
        var user = await _userQuery.GetByEmailAsync(email);
        if (user == null || user.PhoneNumber != phoneNumber)
            return "إذا كانت هذه البيانات مسجلة، فستتلقى رمزاً لإعادة تعيين كلمة المرور.";

        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        user.PasswordResetToken          = token;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var subject = "استعادة كلمة المرور - Luxira Chat";
        var body = $@"
            <div dir='rtl' style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px; padding: 24px;'>
                <h2 style='color: #009688;'>استعادة كلمة المرور لمساحة عمل Luxira</h2>
                <p>مرحباً {user.FullName}،</p>
                <p>لقد طلبنا رمزاً لإعادة تعيين كلمة المرور الخاصة بك. يرجى استخدام الرمز التالي لإعادة التعيين:</p>
                <div style='background-color: #f4f7f6; padding: 16px; border-radius: 8px; text-align: center; font-size: 24px; font-weight: bold; color: #009688; letter-spacing: 2px; margin: 24px 0;'>
                    {token}
                </div>
                <p style='color: #64748b; font-size: 14px;'>ينتهي صلاحية هذا الرمز خلال ساعة واحدة.</p>
                <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;' />
                <p style='color: #64748b; font-size: 12px; text-align: center;'>جميع الحقوق محفوظة . 2026 Luxira Holding ©</p>
            </div>";

        await _emailService.SendEmailAsync(email, subject, body);

        return "تم إرسال رمز استعادة كلمة المرور إلى بريدك الإلكتروني بنجاح.";
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userQuery.GetByEmailAsync(email);
        if (user == null ||
            string.IsNullOrEmpty(user.PasswordResetToken) ||
            !user.PasswordResetToken.Trim().Equals(token?.Trim(), StringComparison.OrdinalIgnoreCase) ||
            user.PasswordResetTokenExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired reset token.");

        user.PasswordHash               = _passwordHasher.HashPassword(newPassword);
        user.PasswordResetToken          = null;
        user.PasswordResetTokenExpiresAt = null;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    // ─────────────────── ADMIN USER MANAGEMENT ───────────────────

    public async Task<UserDto> CreateUserAsync(string phoneNumber, string password, string fullName, string? email, Guid createdByAdminId)
    {
        if (await _unitOfWork.Users.GetByPhoneNumberAsync(phoneNumber) != null)
            throw new Exception("User with this phone number already exists.");
        if (email != null && await _userQuery.EmailExistsAsync(email))
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
            Id = Guid.NewGuid(), UserId = userId, BlockedByAdminId = adminId,
            Reason = reason, BlockedAt = DateTime.UtcNow
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
        var users = await _userQuery.GetAllUsersAsync();
        return users.Select(MapToDto);
    }

    // ─────────────────── PROFILES ───────────────────

    public async Task<UserPublicProfileDto?> GetPublicProfileAsync(Guid viewerId, Guid targetUserId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(targetUserId);
        if (user == null) return null;

        var sharedMedia = await _userQuery.GetSharedMediaAsync(targetUserId, viewerId);

        return new UserPublicProfileDto(
            user.Id, user.FullName, user.ProfileImageUrl,
            user.IsVerified, user.IsOnline, user.LastSeenAt, sharedMedia);
    }

    public async Task<UserDto?> GetProfileAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user == null ? null : MapToDto(user);
    }

    public async Task UpdateProfileAsync(Guid userId, string? fullName, string? profileImageUrl)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId) ?? throw new Exception("User not found.");
        if (fullName != null)        user.FullName        = fullName;
        if (profileImageUrl != null) user.ProfileImageUrl = profileImageUrl;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    // ─────────────────── PRIVATE CHATS ───────────────────

    public async Task<GroupDto> CreatePrivateChatAsync(Guid adminId, Guid targetUserId)
    {
        var targetUser = await _unitOfWork.Users.GetByIdAsync(targetUserId)
            ?? throw new Exception("Target user not found.");

        if (await _groupQuery.PrivateChatExistsAsync(adminId, targetUserId))
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

        // Admin: not muted. Target user: muted by default (cannot reply until permission granted).
        await _unitOfWork.GroupMembers.AddAsync(new GroupMember { Id = Guid.NewGuid(), GroupId = group.Id, UserId = adminId, IsMuted = false, JoinedAt = DateTime.UtcNow, AddedByAdminId = adminId });
        await _unitOfWork.GroupMembers.AddAsync(new GroupMember { Id = Guid.NewGuid(), GroupId = group.Id, UserId = targetUserId, IsMuted = true, JoinedAt = DateTime.UtcNow, AddedByAdminId = adminId });

        await _unitOfWork.SaveChangesAsync();
        return new GroupDto(group.Id, group.Name, group.ImageUrl, group.CreatedAt, IsPrivate: true, PrivateTargetUserId: targetUserId);
    }

    // ─────────────────── FAVOURITES ───────────────────

    public async Task ToggleFavoriteGroupAsync(Guid userId, Guid groupId)
    {
        if (await _groupQuery.FavoriteExistsAsync(userId, groupId))
            await _groupQuery.RemoveFavoriteAsync(userId, groupId);
        else
            await _groupQuery.AddFavoriteAsync(userId, groupId);
    }

    public async Task<IEnumerable<GroupDto>> GetFavoriteGroupsAsync(Guid userId)
        => await _groupQuery.GetFavoriteGroupsAsync(userId);

    // ─────────────────── HELPERS ───────────────────
    private static UserDto MapToDto(User u) => new(
        u.Id, u.PhoneNumber, u.FullName, u.ProfileImageUrl,
        u.Role, u.Status, u.IsOnline, u.LastSeenAt,
        u.IsVerified, u.CanReceivePrivateMessages);
}
