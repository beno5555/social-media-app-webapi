namespace aspnetproject.BusinessLogic.Dtos.CommentDtos;

public record CreateCommentDto(string CommentContent, int CommenterUserId, int PostId);