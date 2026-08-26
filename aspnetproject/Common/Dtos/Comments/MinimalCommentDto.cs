using System.Security.Principal;

namespace aspnetproject.Common.Dtos.Comments;

public class MinimalCommentDto
{
    public int    Id             { get; set; }
    public string Content        { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
}