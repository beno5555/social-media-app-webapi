using aspnetproject.BusinessLogic.Dtos.CommentDtos;
using aspnetproject.BusinessLogic.Mappers.Base;
using aspnetproject.Common.Dtos.Comments;
using aspnetproject.Models;

namespace aspnetproject.BusinessLogic.Mappers;

public class CommentMapper : IMapper<Comment, CreateCommentDto, DisplayCommentDto>
{
    public Comment ToEntity(CreateCommentDto createCommentDto)
    {
        return new Comment
        {
            CommentContent = createCommentDto.CommentContent,
            CommenterUserId = createCommentDto.CommenterUserId,
            PostId = createCommentDto.PostId,
        };
    }

    public DisplayCommentDto ToDisplay(Comment comment)
    {
        return new DisplayCommentDto
        {
            Id = comment.Id,
            AuthorUsername = comment.CommenterUser!.Username,
            Content = comment.CommentContent,
            UploadedAt = comment.CreatedAt
        };
    }
}