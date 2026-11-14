using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Moderation;
using aspnetproject.Infrastructure.Dtos.Reports;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Mappers;

// An administrator's view of content under review. Used by the report list, and meant for the moderation queue to reuse.
public static class SnapshotMapper
{
    /// <summary>
    /// assumes applied .Include() on User and Images
    /// </summary>
    public static PostSnapshotDto ToPostSnapshot(Post post)
    {
        return new PostSnapshotDto
        {
            Id          = post.Id,
            Title       = post.PostTitle,
            PostContent = post.PostContent,
            Images      = ImageMapper.ToImageDtos(post.Images),
            Author      = new MinimalUserDto
            {
                Id       = post.UserId,
                Username = UsernameMapper.ResolveUsername(post.User, post.UserId)
            },
            UploadedAt = post.CreatedAt,
            IsHidden   = post.IsHidden
        };
    }

    /// <summary>
    /// assumes applied .Include() on CommenterUser
    /// </summary>
    public static CommentSnapshotDto ToCommentSnapshot(Comment comment)
    {
        return new CommentSnapshotDto
        {
            Id      = comment.Id,
            Content = comment.CommentContent,
            Author  = new MinimalUserDto
            {
                Id       = comment.CommenterUserId,
                Username = UsernameMapper.ResolveUsername(comment.CommenterUser, comment.CommenterUserId)
            },
            PostId     = comment.PostId,
            UploadedAt = comment.CreatedAt,
            IsHidden   = comment.IsHidden
        };
    }

    // The real username even for a deactivated or soft-deleted account: the administrator must know whom the report is about.
    public static UserSnapshotDto ToUserSnapshot(User user)
    {
        return new UserSnapshotDto
        {
            Id       = user.Id,
            Username = user.Username,
            Bio      = user.Bio
        };
    }
}
