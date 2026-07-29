namespace InternalChat.Application.DTOs;

public record CreateGroupRequest(string Name, string? ImageUrl);
public record CreatePrivateChatRequest(Guid TargetUserId);
public record AddMembersRequest(List<Guid> UserIds);
public record MuteMemberRequest(bool IsMuted);

public record GroupDto(
    Guid Id,
    string Name,
    string? ImageUrl,
    DateTime CreatedAt,
    bool IsPrivate = false,
    Guid? PrivateTargetUserId = null,
    bool IsFavorite = false,
    int UnreadCount = 0,
    string? LastMessage = null,
    DateTime? LastMessageAt = null);

public record GroupMemberDto(
    Guid UserId,
    string FullName,
    string? ProfileImageUrl,
    string Role,
    bool IsOnline,
    DateTime? LastSeenAt,
    bool IsMuted,
    bool IsVerified = false);
