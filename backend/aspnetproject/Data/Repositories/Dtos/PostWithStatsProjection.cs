using aspnetproject.Data.Models;

namespace aspnetproject.Data.Repositories.Dtos;

// A post with the counts every post read returns, computed in the same query as the post.
public class PostWithStatsProjection
{
    public Post Post          { get; set; } = null!;
    public int  LikeCount     { get; set; }
    public int  CommentCount  { get; set; }
    public bool LikedByCaller { get; set; }
}
