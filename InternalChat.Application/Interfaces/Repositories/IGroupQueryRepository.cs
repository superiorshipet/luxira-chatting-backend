using InternalChat.Application.DTOs;
using InternalChat.Domain.Entities;

namespace InternalChat.Application.Interfaces.Repositories;

/// <summary>
/// Complex read queries for Groups and Messages.
/// Lives in Application, implemented in Infrastructure using EF Core.
/// </summary>
public interface IGroupQueryRepository
{
    Task<IEnumerable<GroupDto>> GetUserGroupsWithMetaAsync(Guid userId, string? filter);
    Task<IEnumerable<GroupDto>> GetFavoriteGroupsAsync(Guid userId);
    Task<bool> FavoriteExistsAsync(Guid userId, Guid groupId);
    Task AddFavoriteAsync(Guid userId, Guid groupId);
    Task RemoveFavoriteAsync(Guid userId, Guid groupId);
    Task<IEnumerable<MessageDto>> SearchMessagesAsync(Guid userId, string keyword);
    Task<bool> PrivateChatExistsAsync(Guid adminId, Guid targetUserId);
}
