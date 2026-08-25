namespace aspnetproject.BusinessLogic.Dtos.Posts;

public class SummarizedPostDisplayDto
{
    public int    Id    { get; set; }
    public string Title { get; set; } = string.Empty;
    
    public int      UserId     { get; set; }
    public DateTime UploadedAt { get; set; }
}