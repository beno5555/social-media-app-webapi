using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Dtos.WebsocketsTransfer;

namespace aspnetproject.Infrastructure.Mappers;

public static class PostLikeMapper
{
    public static PostLike ToEntity(int postId, int userId)
    {
        return new PostLike
        {
            PostId = postId,
            UserId = userId
        };
    }

    // A deactivated or soft-deleted liker keeps their place in the list under a placeholder name.
    public static MinimalUserDto ToLikerDisplay(PostLike like)
    {
        return new MinimalUserDto
        {
            Id       = like.UserId,
            Username = UsernameMapper.ResolveUsername(like.User, like.UserId)
        };
    }

    public static PostLikedDto ToNotification(PostLike like, int likeCount)
    {
        return new PostLikedDto
        {
            PostId    = like.PostId,
            LikedBy   = ToLikerDisplay(like),
            LikeCount = likeCount
        };
    }
}
