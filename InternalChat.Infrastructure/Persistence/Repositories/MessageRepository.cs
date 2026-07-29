using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalChat.Infrastructure.Persistence.Repositories;

/// <summary>
/// Concrete repository for Message entity.
/// </summary>
public class MessageRepository : EfRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Message>> GetPageAsync(Guid groupId, DateTime beforeCursor, int take)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Where(m => m.GroupId == groupId && m.SentAt < beforeCursor)
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageEditHistory>> GetEditHistoryAsync(Guid messageId)
    {
        return await _context.MessageEditHistories
            .Where(h => h.MessageId == messageId)
            .OrderByDescending(h => h.EditedAt)
            .ToListAsync();
    }
}
