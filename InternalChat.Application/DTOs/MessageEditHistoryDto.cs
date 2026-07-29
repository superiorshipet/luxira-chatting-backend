namespace InternalChat.Application.DTOs;

public record MessageEditHistoryDto(Guid Id, Guid MessageId, string OldContent, DateTime EditedAt);
