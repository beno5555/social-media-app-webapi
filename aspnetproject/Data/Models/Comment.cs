namespace aspnetproject.Data.Models;

public class Comment : BaseEntity
{
    public string CommentContent { get; set; } = string.Empty;

    public int?  CommenterUserId { get; set; }
    public User? CommenterUser   { get; set; }

    public int   PostId { get; set; }
    public Post? Post   { get; set; } 
}