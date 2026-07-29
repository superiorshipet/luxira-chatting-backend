using InternalChat.Domain.Entities;

namespace InternalChat.Application.Interfaces.Repositories;

/// <summary>
/// Repository for MessageRead entities.
/// </summary>
public interface IMessageReadRepository
{
    Task AddAsync(MessageRead messageRead);
    Task<IEnumerable<MessageRead>> GetByMessageIdAsync(Guid messageId);
    Task<IEnumerable<MessageRead>> GetByUserIdAsync(Guid userId);
}
