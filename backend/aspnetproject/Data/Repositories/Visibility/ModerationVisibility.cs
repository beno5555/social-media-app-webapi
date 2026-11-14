using System.Linq.Expressions;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;

namespace aspnetproject.Data.Repositories.Visibility;

/// <summary>
/// The visibility rule every post and comment read applies, as filters EF translates to SQL.
/// Hidden content is readable only by its author and by administrators; flagged content reads as visible.
/// A guest has no caller id and an orphaned comment has no author, so the author check never matches null to null.
/// </summary>
public static class ModerationVisibility
{
    public static Expression<Func<Post, bool>> PostReadableBy(int? callerId, bool isAdmin)
    {
        return post => post.ModerationStatus != ModerationStatus.Hidden
                       || isAdmin
                       || (callerId != null && post.UserId == callerId);
    }

    // The comment alone, for reads that have already checked its post.
    public static Expression<Func<Comment, bool>> CommentReadableBy(int? callerId, bool isAdmin)
    {
        return comment => comment.ModerationStatus != ModerationStatus.Hidden
                          || isAdmin
                          || (callerId != null && comment.CommenterUserId == callerId);
    }

    // A comment read on its own is readable only when its post is too, so it cannot reveal a hidden post.
    public static Expression<Func<Comment, bool>> CommentAndPostReadableBy(int? callerId, bool isAdmin)
    {
        return comment => (comment.ModerationStatus != ModerationStatus.Hidden
                           || isAdmin
                           || (callerId != null && comment.CommenterUserId == callerId))
                          && (comment.Post!.ModerationStatus != ModerationStatus.Hidden
                           || isAdmin
                           || (callerId != null && comment.Post.UserId == callerId));
    }
}
