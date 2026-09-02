using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Comments;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Mappers;

public static class CommentMapper 
{
    public static Comment ToEntity(int authorId, int postId, CreateCommentDto createCommentDto)
    {
        return new Comment
        {
            CommentContent = createCommentDto.Content,
            CommenterUserId = authorId,
            PostId =  postId,
        };
    }

    public static StandardCommentDto ToStandardDisplay(Comment comment)
    {
        return new StandardCommentDto
        {
            Id = comment.Id,
            Author = new MinimalUserDto
            {
                Id = comment.CommenterUserId,
                Username = UsernameMapper.ResolveUsername(comment.CommenterUser, comment.CommenterUserId),
            },
            Content = comment.CommentContent,
            UploadedAt = comment.CreatedAt
        };
    }

    public static FullCommentDto ToFullDisplay(Comment comment)
    {
        return new FullCommentDto
        {
            Id = comment.Id,
            Content = comment.CommentContent,

            Author = new MinimalUserDto
            {
                Id = comment.CommenterUserId,
                Username = UsernameMapper.ResolveUsername(comment.CommenterUser, comment.CommenterUserId),
            },
            PostId = comment.PostId,

            UploadedAt = comment.CreatedAt,
            LastUpdatedAt = comment.LastUpdatedAt
        };
    }

    public static NotifyCommentDto ToNotification(Comment comment)
    {
        return new NotifyCommentDto
        {
            Id = comment.Id,
            Content = comment.CommentContent,
            AuthorUsername = UsernameMapper.ResolveUsername(comment.CommenterUser, comment.CommenterUserId),
        };
    }
}