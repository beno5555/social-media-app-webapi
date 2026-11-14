using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Moderation;

// What an administrator needs to judge a comment without opening it. Shared by the moderation queue and the report list.
public class CommentSnapshotDto
{
    public int    Id   { get; set; }
    public string Text { get; set; } = string.Empty;

    public MinimalUserDto Author     { get; set; } = null!;
    public int            PostId     { get; set; }
    public DateTime       UploadedAt { get; set; }
}
