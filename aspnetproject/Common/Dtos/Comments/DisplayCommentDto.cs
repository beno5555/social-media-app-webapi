namespace aspnetproject.Common.Dtos.Comments;

public class DisplayCommentDto
{
    public int      Id             { get; set; }
    public string   AuthorUsername { get; set; } = string.Empty;
    public string   Content        { get; set; } = string.Empty;
    public DateTime UploadedAt     { get; set; }
}