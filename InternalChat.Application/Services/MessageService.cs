using InternalChat.Application.DTOs;
using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Application.Interfaces.Services;
using InternalChat.Domain.Entities;
using InternalChat.Domain.Enums;

namespace InternalChat.Application.Services;

public class MessageService : IMessageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public MessageService(IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<MessageDto> SendMessageAsync(Guid groupId, Guid senderId, string? content, MessageType type, string? attachmentUrl, Guid? replyToMessageId)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            SenderId = senderId,
            Content = content,
            MessageType = type,
            SentAt = DateTime.UtcNow,
            IsEdited = false,
            IsDeleted = false,
            IsPinned = false,
            ReplyToMessageId = replyToMessageId
        };
        
        if (!string.IsNullOrEmpty(attachmentUrl))
        {
            message.Attachments.Add(new Attachment
            {
                Id = Guid.NewGuid(),
                FileUrl = attachmentUrl,
                FileType = "unknown",
                FileSizeBytes = 0
            });
        }

        await _unitOfWork.Messages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();
        
        var sender = await _unitOfWork.Users.GetByIdAsync(senderId);

        return new MessageDto(
            message.Id,
            message.GroupId,
            message.SenderId,
            sender?.FullName ?? "Unknown",
            message.Content,
            message.MessageType,
            message.SentAt,
            message.IsEdited,
            message.IsDeleted,
            message.IsPinned,
            message.ReplyToMessageId,
            message.ForwardedFromMessageId,
            message.ForwardedFromGroupId,
            message.Attachments.Select(a => new AttachmentDto(a.FileUrl, a.FileType, a.FileSizeBytes, a.ThumbnailUrl, a.DurationSeconds))
        );
    }

    public async Task<MessageDto> EditMessageAsync(Guid messageId, Guid senderId, string newContent)
    {
        var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
        if (message == null) throw new Exception("Message not found.");
        if (message.SenderId != senderId) throw new UnauthorizedAccessException("Not original sender.");
        if (message.IsDeleted) throw new Exception("Cannot edit a deleted message.");

        var history = new MessageEditHistory
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            OldContent = message.Content ?? "",
            EditedAt = DateTime.UtcNow
        };
        
        message.Content = newContent;
        message.IsEdited = true;
        
        message.EditHistories.Add(history);
        
        _unitOfWork.Messages.Update(message);
        await _unitOfWork.SaveChangesAsync();
        
        return new MessageDto(
            message.Id, message.GroupId, message.SenderId, "", message.Content, message.MessageType, 
            message.SentAt, message.IsEdited, message.IsDeleted, message.IsPinned, message.ReplyToMessageId, 
            message.ForwardedFromMessageId, message.ForwardedFromGroupId, new List<AttachmentDto>()
        );
    }

    public async Task DeleteMessageAsync(Guid messageId, Guid callerUserId)
    {
        var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
        if (message == null) throw new Exception("Message not found.");

        var caller = await _unitOfWork.Users.GetByIdAsync(callerUserId);
        
        if (message.SenderId != callerUserId && caller?.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("Cannot delete this message.");

        message.IsDeleted = true;
        _unitOfWork.Messages.Update(message);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task PinMessageAsync(Guid messageId, Guid callerUserId, bool isPinned)
    {
        var caller = await _unitOfWork.Users.GetByIdAsync(callerUserId);
        if (caller == null || caller.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("Only admins can pin messages.");

        var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
        if (message == null) throw new Exception("Message not found.");

        message.IsPinned = isPinned;
        _unitOfWork.Messages.Update(message);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(Guid messageId, Guid userId)
    {
        var reads = await _unitOfWork.MessageReads.GetByMessageIdAsync(messageId);
        if (!reads.Any(r => r.UserId == userId))
        {
            await _unitOfWork.MessageReads.AddAsync(new MessageRead
            {
                MessageId = messageId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task ReactToMessageAsync(Guid messageId, Guid userId, string emoji)
    {
        var existing = await _unitOfWork.MessageReactions.GetByUserAndMessageAsync(userId, messageId);
        if (existing != null)
        {
            if (existing.Emoji == emoji)
            {
                _unitOfWork.MessageReactions.Remove(existing);
            }
            else
            {
                existing.Emoji = emoji;
                existing.ReactedAt = DateTime.UtcNow;
                _unitOfWork.MessageReactions.Update(existing);
            }
        }
        else
        {
            await _unitOfWork.MessageReactions.AddAsync(new MessageReaction
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji,
                ReactedAt = DateTime.UtcNow
            });
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<MessageDto>> ForwardMessageAsync(Guid messageId, Guid forwarderId, IEnumerable<Guid> targetGroupIds)
    {
        var sourceMessage = await _unitOfWork.Messages.GetByIdAsync(messageId);
        if (sourceMessage == null) throw new Exception("Message not found.");

        var forwardedMessages = new List<MessageDto>();
        
        foreach (var groupId in targetGroupIds)
        {
            var isMember = await _unitOfWork.GroupMembers.IsActiveMemberAsync(groupId, forwarderId);
            if (!isMember) continue;

            var newMessage = new Message
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                SenderId = forwarderId,
                Content = sourceMessage.Content,
                MessageType = sourceMessage.MessageType,
                SentAt = DateTime.UtcNow,
                IsEdited = false,
                IsDeleted = false,
                IsPinned = false,
                ForwardedFromMessageId = sourceMessage.Id,
                ForwardedFromGroupId = sourceMessage.GroupId
            };

            await _unitOfWork.Messages.AddAsync(newMessage);
            
            forwardedMessages.Add(new MessageDto(
                newMessage.Id, newMessage.GroupId, newMessage.SenderId, "Forwarder", newMessage.Content, 
                newMessage.MessageType, newMessage.SentAt, newMessage.IsEdited, newMessage.IsDeleted, newMessage.IsPinned, 
                null, newMessage.ForwardedFromMessageId, newMessage.ForwardedFromGroupId, new List<AttachmentDto>()
            ));
        }
        
        await _unitOfWork.SaveChangesAsync();
        return forwardedMessages;
    }

    public async Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid groupId, Guid callerUserId, DateTime beforeCursor, int take)
    {
        var isMember = await _unitOfWork.GroupMembers.IsActiveMemberAsync(groupId, callerUserId);
        if (!isMember) throw new UnauthorizedAccessException("Not a member of this group.");
        
        var caller = await _unitOfWork.Users.GetByIdAsync(callerUserId);
        var isAdmin = caller?.Role == UserRole.Admin;

        var messages = await _unitOfWork.Messages.GetPageAsync(groupId, beforeCursor, take);
        
        return messages.Select(m => {
            var content = m.Content;
            // Normal users don't see deleted content
            if (m.IsDeleted && !isAdmin)
            {
                content = null;
            }

            return new MessageDto(
                m.Id, m.GroupId, m.SenderId, m.Sender?.FullName ?? "Unknown", content, m.MessageType, 
                m.SentAt, m.IsEdited, m.IsDeleted, m.IsPinned, m.ReplyToMessageId, m.ForwardedFromMessageId, m.ForwardedFromGroupId, 
                m.Attachments.Select(a => new AttachmentDto(a.FileUrl, a.FileType, a.FileSizeBytes, a.ThumbnailUrl, a.DurationSeconds))
            );
        });
    }

    public async Task<IEnumerable<MessageEditHistoryDto>> GetMessageEditHistoryAsync(Guid messageId, Guid callerUserId)
    {
        var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
        if (message == null) throw new Exception("Message not found.");
        
        var isMember = await _unitOfWork.GroupMembers.IsActiveMemberAsync(message.GroupId, callerUserId);
        if (!isMember) throw new UnauthorizedAccessException("Not a member of this group.");
        
        var history = await _unitOfWork.Messages.GetEditHistoryAsync(messageId);
        
        return history.Select(h => new MessageEditHistoryDto(h.Id, h.MessageId, h.OldContent, h.EditedAt));
    }
}
