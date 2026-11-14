using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Comments;

public class StandardCommentDto
{
    public int    Id             { get; set; }
    public string Content        { get; set; } = string.Empty;

    public MinimalUserDto Author     { get; set; } = null!;
    public DateTime       UploadedAt { get; set; }

    // True only for the author or an administrator: nobody else is ever sent hidden content.
    public bool IsHidden { get; set; }
}