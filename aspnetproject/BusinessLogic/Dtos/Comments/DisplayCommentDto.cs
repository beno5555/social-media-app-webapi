namespace aspnetproject.BusinessLogic.Dtos.CommentDtos;

public record DisplayCommentDto(int Id, string SenderUsername, string Content, DateTime SentAt);