using aspnetproject.BusinessLogic.Dtos.CommentDtos;

namespace aspnetproject.BusinessLogic.Dtos.PostDtos;

public record DisplayPostDto(int Id, string AuthorUsername, string Title, string Content, DateTime UploadedAt, List<DisplayCommentDto>? Comments = null);