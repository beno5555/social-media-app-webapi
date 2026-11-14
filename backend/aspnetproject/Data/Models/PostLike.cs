namespace aspnetproject.Data.Models;

// One like per user per post, enforced by a unique index on (PostId, UserId).
public class PostLike : BaseEntity
{
    public int   PostId { get; set; }
    public Post? Post   { get; set; }

    public int   UserId { get; set; }
    public User? User   { get; set; }
}
