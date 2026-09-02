using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Comments;

public class StandardCommentDto
{
    public int    Id             { get; set; }
    public string Content        { get; set; } = string.Empty;

    public MinimalUserDto Author { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
}