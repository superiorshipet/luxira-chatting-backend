namespace InternalChat.Application.DTOs;

public record CreateGroupRequest(string Name, string? ImageUrl);
public record AddMembersRequest(List<Guid> UserIds);
public record MuteMemberRequest(bool IsMuted);

public record GroupDto(Guid Id, string Name, string? ImageUrl, DateTime CreatedAt);
public record GroupMemberDto(Guid UserId, string FullName, string? ProfileImageUrl, string Role, bool IsOnline, DateTime? LastSeenAt, bool IsMuted);
