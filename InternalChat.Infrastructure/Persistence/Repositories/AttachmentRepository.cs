using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Domain.Entities;

namespace InternalChat.Infrastructure.Persistence.Repositories;

/// <summary>
/// Concrete repository for Attachment entity.
/// </summary>
public class AttachmentRepository : EfRepository<Attachment>, IAttachmentRepository
{
    public AttachmentRepository(AppDbContext context) : base(context)
    {
    }
}
