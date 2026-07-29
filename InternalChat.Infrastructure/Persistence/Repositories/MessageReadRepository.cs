using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalChat.Infrastructure.Persistence.Repositories;

/// <summary>
/// Concrete repository for MessageRead entity.
/// </summary>
public class MessageReadRepository : IMessageReadRepository
{
    private readonly AppDbContext _context;

    public MessageReadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MessageRead messageRead)
    {
        await _context.MessageReads.AddAsync(messageRead);
    }

    public async Task<IEnumerable<MessageRead>> GetByMessageIdAsync(Guid messageId)
    {
        return await _context.MessageReads
            .Where(mr => mr.MessageId == messageId)
            .ToListAsync();
    }
}
