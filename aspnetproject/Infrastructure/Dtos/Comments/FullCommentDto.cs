using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Common.Dtos.Comments;

public class FullCommentDto
{
    public int    Id      { get; set; }
    public string Content { get; set; } = string.Empty;

    public MinimalUserDto Author { get; set; } = null!;
    public int              PostId      { get; set; }

    public DateTime UploadedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}