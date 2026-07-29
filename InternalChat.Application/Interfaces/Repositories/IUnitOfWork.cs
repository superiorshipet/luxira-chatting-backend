namespace InternalChat.Application.Interfaces.Repositories;

/// <summary>
/// Provides a single point of commit for multiple repository operations.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IGroupRepository Groups { get; }
    IGroupMemberRepository GroupMembers { get; }
    IMessageRepository Messages { get; }
    IMessageReadRepository MessageReads { get; }
    IMessageReactionRepository MessageReactions { get; }
    IAttachmentRepository Attachments { get; }
    IUserBlockRepository UserBlocks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
