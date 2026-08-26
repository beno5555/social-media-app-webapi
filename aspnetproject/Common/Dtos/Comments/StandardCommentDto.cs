namespace aspnetproject.Common.Dtos.Comments;

public class StandardCommentDto
{
    public int    Id             { get; set; }
    public string Content        { get; set; } = string.Empty;

    public int AuthorId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }
}