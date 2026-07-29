using InternalChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternalChat.Infrastructure.Persistence;

/// <summary>
/// Handles all read/write access to the database using EF Core.
/// No business rules live here — only persistence.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<GroupMember> GroupMembers { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<MessageEditHistory> MessageEditHistories { get; set; } = null!;
    public DbSet<MessageRead> MessageReads { get; set; } = null!;
    public DbSet<MessageReaction> MessageReactions { get; set; } = null!;
    public DbSet<Attachment> Attachments { get; set; } = null!;
    public DbSet<UserBlock> UserBlocks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();
            
        modelBuilder.Entity<User>()
            .HasOne(u => u.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(u => u.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Group
        modelBuilder.Entity<Group>()
            .HasOne(g => g.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(g => g.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // GroupMember
        modelBuilder.Entity<GroupMember>()
            .HasIndex(gm => new { gm.GroupId, gm.UserId })
            .IsUnique()
            .HasFilter("\"RemovedAt\" IS NULL");
            
        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.User)
            .WithMany()
            .HasForeignKey(gm => gm.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.AddedByAdmin)
            .WithMany()
            .HasForeignKey(gm => gm.AddedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Message
        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.GroupId, m.SentAt });
            
        modelBuilder.Entity<Message>()
            .HasIndex(m => m.ForwardedFromMessageId);
            
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.ReplyToMessage)
            .WithMany()
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.ForwardedFromMessage)
            .WithMany()
            .HasForeignKey(m => m.ForwardedFromMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        // MessageRead
        modelBuilder.Entity<MessageRead>()
            .HasKey(mr => new { mr.MessageId, mr.UserId });

        modelBuilder.Entity<MessageRead>()
            .HasIndex(mr => mr.UserId);
            
        modelBuilder.Entity<MessageRead>()
            .HasOne(mr => mr.User)
            .WithMany()
            .HasForeignKey(mr => mr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // MessageReaction
        modelBuilder.Entity<MessageReaction>()
            .HasIndex(mr => new { mr.MessageId, mr.UserId })
            .IsUnique();
            
        modelBuilder.Entity<MessageReaction>()
            .HasOne(mr => mr.User)
            .WithMany()
            .HasForeignKey(mr => mr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserBlock
        modelBuilder.Entity<UserBlock>()
            .HasOne(ub => ub.User)
            .WithMany()
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<UserBlock>()
            .HasOne(ub => ub.BlockedByAdmin)
            .WithMany()
            .HasForeignKey(ub => ub.BlockedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Cascade delete from Message to its children
        modelBuilder.Entity<Message>()
            .HasMany(m => m.Attachments)
            .WithOne(a => a.Message)
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasMany(m => m.Reactions)
            .WithOne(r => r.Message)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasMany(m => m.Reads)
            .WithOne(mr => mr.Message)
            .HasForeignKey(mr => mr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Message>()
            .HasMany(m => m.EditHistories)
            .WithOne(e => e.Message)
            .HasForeignKey(e => e.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
