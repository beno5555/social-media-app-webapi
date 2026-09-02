namespace aspnetproject.Infrastructure.Dtos.Posts;

public class MinimalPostDisplayDto
{
    public int    Id    { get; set; }
    public string Title { get; set; } = string.Empty;

    public int     UserId     { get; set; }
    public DateTime UploadedAt { get; set; }
}