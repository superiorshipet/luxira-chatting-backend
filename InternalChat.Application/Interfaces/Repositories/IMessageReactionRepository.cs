using InternalChat.Domain.Common;
using InternalChat.Domain.Entities;

namespace InternalChat.Application.Interfaces.Repositories;

/// <summary>
/// Repository for MessageReaction entities.
/// </summary>
public interface IMessageReactionRepository : IRepository<MessageReaction>
{
    Task<MessageReaction?> GetByUserAndMessageAsync(Guid userId, Guid messageId);
    Task<IEnumerable<MessageReaction>> GetByMessageIdAsync(Guid messageId);
}
