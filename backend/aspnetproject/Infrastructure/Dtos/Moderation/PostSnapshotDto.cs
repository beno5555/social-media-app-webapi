using aspnetproject.Infrastructure.Dtos.Images;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Moderation;

// What an administrator sees of a post under review. Used by the report list, and meant for the moderation queue to reuse.
public class PostSnapshotDto
{
    public int    Id          { get; set; }
    public string Title       { get; set; } = string.Empty;
    public string PostContent { get; set; } = string.Empty;

    // In upload order.
    public List<ImageDto> Images { get; set; } = [];

    public MinimalUserDto Author     { get; set; } = null!;
    public DateTime       UploadedAt { get; set; }

    public bool IsHidden { get; set; }
}
