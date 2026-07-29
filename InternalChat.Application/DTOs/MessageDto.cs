using InternalChat.Domain.Enums;

namespace InternalChat.Application.DTOs;

public record MessageDto(
    Guid Id,
    Guid GroupId,
    Guid SenderId,
    string SenderName,
    string? Content,
    MessageType MessageType,
    DateTime SentAt,
    bool IsEdited,
    bool IsDeleted,
    bool IsPinned,
    Guid? ReplyToMessageId,
    Guid? ForwardedFromMessageId,
    Guid? ForwardedFromGroupId,
    IEnumerable<AttachmentDto> Attachments
);

public record AttachmentDto(string FileUrl, string FileType, long FileSizeBytes, string? ThumbnailUrl, int? DurationSeconds);
