namespace aspnetproject.BusinessLogic.Dtos.Posts;

public class CreatePostDto(string PostTitle, string PostContent)
{
    public string PostTitle   { get; set; } = string.Empty;
    public string PostContent { get; set; } = string.Empty;
}