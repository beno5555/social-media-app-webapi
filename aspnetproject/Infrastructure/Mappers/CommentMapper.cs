using aspnetproject.BusinessLogic.Mappers.Base;
using aspnetproject.Common.Dtos.Comments;
using aspnetproject.Common.Dtos.Users;
using aspnetproject.Infrastructure.Dtos.Comments;
using aspnetproject.Models;

namespace aspnetproject.BusinessLogic.Mappers;

public class CommentMapper 
{
    public Comment ToEntity(int authorId, CreateCommentDto createCommentDto)
    {
        return new Comment
        {
            CommentContent = createCommentDto.Content,
            CommenterUserId = authorId,
            PostId = createCommentDto.PostId,
        };
    }

    public MinimalCommentDto ToMinimalDisplay(Comment comment)
    {
        return new MinimalCommentDto
        {
            Id = comment.Id,
            AuthorUsername = comment.CommenterUser!.Username,
            Content = comment.CommentContent,
        };
    }

    public StandardCommentDto ToStandardDisplay(Comment comment)
    {
        return new StandardCommentDto
        {
            Id = comment.Id,
            AuthorId = comment.CommenterUserId,
            AuthorUsername = comment.CommenterUser!.Username,
            Content = comment.CommentContent,
            UploadedAt = comment.CreatedAt
        };
    }

    public FullCommentDto ToFullDisplay(Comment comment)
    {
        return new FullCommentDto
        {
            Id = comment.Id,
            Content = comment.CommentContent,

            Author = new MinimalUserDto
            {
                Id = comment.CommenterUserId,
                Username = comment.CommenterUser!.Username
            },
            PostId = comment.PostId,

            UploadedAt = comment.CreatedAt,
            LastUpdatedAt = comment.LastUpdatedAt
        };
    }
}