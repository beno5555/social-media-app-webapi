using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Moderation;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Mappers;

// The snapshots an administrator judges content by. Shared by the moderation queue and the report list.
public static class ModerationMapper
{
    // Assumes the author and the images were loaded.
    public static PostSnapshotDto ToPostSnapshot(Post post)
    {
        return new PostSnapshotDto
        {
            Id = post.Id,
            Title = post.PostTitle,
            Text = post.PostContent,
            Images = ImageMapper.ToImageDtos(post.Images),

            Author = new MinimalUserDto
            {
                Id = post.UserId,
                Username = UsernameMapper.ResolveUsername(post.User, post.UserId)
            },

            UploadedAt = post.CreatedAt
        };
    }

    // Assumes the author was loaded.
    public static CommentSnapshotDto ToCommentSnapshot(Comment comment)
    {
        return new CommentSnapshotDto
        {
            Id = comment.Id,
            Text = comment.CommentContent,

            Author = new MinimalUserDto
            {
                Id = comment.CommenterUserId,
                Username = UsernameMapper.ResolveUsername(comment.CommenterUser, comment.CommenterUserId)
            },

            PostId = comment.PostId,
            UploadedAt = comment.CreatedAt
        };
    }
}
