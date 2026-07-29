using InternalChat.Domain.Common;
using InternalChat.Domain.Entities;

namespace InternalChat.Application.Interfaces.Repositories;

/// <summary>
/// Repository for Message entities.
/// </summary>
public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetPageAsync(Guid groupId, DateTime beforeCursor, int take);
    Task<IEnumerable<MessageEditHistory>> GetEditHistoryAsync(Guid messageId);
}
