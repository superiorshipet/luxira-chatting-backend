using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalChat.Infrastructure.Persistence.Repositories;

/// <summary>
/// Concrete repository for MessageReaction entity.
/// </summary>
public class MessageReactionRepository : EfRepository<MessageReaction>, IMessageReactionRepository
{
    public MessageReactionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<MessageReaction?> GetByUserAndMessageAsync(Guid userId, Guid messageId)
    {
        return await _context.MessageReactions
            .SingleOrDefaultAsync(mr => mr.UserId == userId && mr.MessageId == messageId);
    }

    public async Task<IEnumerable<MessageReaction>> GetByMessageIdAsync(Guid messageId)
    {
        return await _context.MessageReactions
            .Include(mr => mr.User)
            .Where(mr => mr.MessageId == messageId)
            .ToListAsync();
    }
}
