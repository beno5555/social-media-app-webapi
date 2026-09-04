using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Comments;

public class FullCommentDto
{
    public int    Id      { get; set; }
    public string Content { get; set; } = string.Empty;
    public int    PostId  { get; set; }

    public MinimalUserDto Author { get; set; } = null!;

    public DateTime  UploadedAt    { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}