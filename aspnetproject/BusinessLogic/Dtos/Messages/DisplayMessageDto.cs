namespace aspnetproject.BusinessLogic.Dtos.MessageDtos;

public record DisplayMessageDto(string MessageContent, string SenderUsername, DateTime SentAt, bool IsRead);