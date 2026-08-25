namespace aspnetproject.BusinessLogic.Dtos.MessageDtos;

public record CreateMessageDto(int SenderId, int ReceiverId, string MessageContent);