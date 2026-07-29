using InternalChat.Application.DTOs;
using InternalChat.Domain.Enums;

namespace InternalChat.Application.Interfaces.Services;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(Guid groupId, Guid senderId, string? content, MessageType type, string? attachmentUrl, Guid? replyToMessageId);
    Task<MessageDto> EditMessageAsync(Guid messageId, Guid senderId, string newContent);
    Task DeleteMessageAsync(Guid messageId, Guid callerUserId);
    Task PinMessageAsync(Guid messageId, Guid callerUserId, bool isPinned);
    Task MarkAsReadAsync(Guid messageId, Guid userId);
    Task MarkGroupAsReadAsync(Guid groupId, Guid userId);
    Task ReactToMessageAsync(Guid messageId, Guid userId, string emoji);
    Task<IEnumerable<MessageDto>> ForwardMessageAsync(Guid messageId, Guid forwarderId, IEnumerable<Guid> targetGroupIds);
    Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid groupId, Guid callerUserId, DateTime beforeCursor, int take);
    Task<IEnumerable<MessageEditHistoryDto>> GetMessageEditHistoryAsync(Guid messageId, Guid callerUserId);
}
