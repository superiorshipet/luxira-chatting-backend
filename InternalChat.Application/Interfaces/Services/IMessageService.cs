using InternalChat.Application.DTOs;
using InternalChat.Domain.Enums;

namespace InternalChat.Application.Interfaces.Services;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(Guid groupId, Guid senderId, string? content, MessageType type, string? attachmentUrl, Guid? replyToMessageId);
    Task<MessageDto> EditMessageAsync(Guid messageId, Guid senderId, string newContent);
    Task MarkAsReadAsync(Guid messageId, Guid userId);
    Task ReactToMessageAsync(Guid messageId, Guid userId, string emoji);
    Task<IEnumerable<MessageDto>> ForwardMessageAsync(Guid messageId, Guid forwarderId, IEnumerable<Guid> targetGroupIds);
    Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid groupId, Guid callerUserId, DateTime beforeCursor, int take);
}
