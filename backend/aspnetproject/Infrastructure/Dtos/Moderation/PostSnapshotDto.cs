using aspnetproject.Infrastructure.Dtos.Images;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Moderation;

// What an administrator needs to judge a post without opening it. Shared by the moderation queue and the report list.
public class PostSnapshotDto
{
    public int    Id    { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text  { get; set; } = string.Empty;

    // In upload order.
    public List<ImageDto> Images { get; set; } = [];

    public MinimalUserDto Author     { get; set; } = null!;
    public DateTime       UploadedAt { get; set; }
}
