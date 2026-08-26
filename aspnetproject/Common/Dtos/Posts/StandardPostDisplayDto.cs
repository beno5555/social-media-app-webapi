namespace aspnetproject.Common.Dtos.Posts;

public class StandardPostDisplayDto
{
    public int    Id    { get; set; }
    public string Title { get; set; } = string.Empty;
    
    public int    UserId   { get; set; }
    public string Username { get; set; } = string.Empty;

    public string   PostContent { get; set; } = string.Empty;
    public DateTime UploadedAt  { get; set; }
}