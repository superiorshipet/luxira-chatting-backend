using InternalChat.Application.Interfaces.Repositories;

namespace InternalChat.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implements IUnitOfWork to commit changes via EF Core.
/// Handles all read/write access to related repositories via a single transaction boundary.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IUserRepository Users { get; }
    public IGroupRepository Groups { get; }
    public IGroupMemberRepository GroupMembers { get; }
    public IMessageRepository Messages { get; }
    public IMessageReadRepository MessageReads { get; }
    public IMessageReactionRepository MessageReactions { get; }
    public IAttachmentRepository Attachments { get; }
    public IUserBlockRepository UserBlocks { get; }

    public UnitOfWork(
        AppDbContext context,
        IUserRepository users,
        IGroupRepository groups,
        IGroupMemberRepository groupMembers,
        IMessageRepository messages,
        IMessageReadRepository messageReads,
        IMessageReactionRepository messageReactions,
        IAttachmentRepository attachments,
        IUserBlockRepository userBlocks)
    {
        _context = context;
        Users = users;
        Groups = groups;
        GroupMembers = groupMembers;
        Messages = messages;
        MessageReads = messageReads;
        MessageReactions = messageReactions;
        Attachments = attachments;
        UserBlocks = userBlocks;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
