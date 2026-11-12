namespace aspnetproject.Infrastructure.Dtos.Comments;

public class NotifyCommentDto
{
    public int    Id             { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string Content        { get; set; } = string.Empty;
    public int      PostId              { get; set; }
}