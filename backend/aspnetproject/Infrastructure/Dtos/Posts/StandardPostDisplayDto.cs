using aspnetproject.Infrastructure.Dtos.Images;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Posts;

public class StandardPostDisplayDto
{
    public int    Id    { get; set; }
    public string Title { get; set; } = string.Empty;

    public MinimalUserDto Author { get; set; } = null!;

    public string   PostContent { get; set; } = string.Empty;
    public DateTime UploadedAt  { get; set; }

    // In upload order.
    public List<ImageDto> Images { get; set; } = [];

    public int  LikeCount    { get; set; }
    public int  CommentCount { get; set; }

    // False for a guest.
    public bool LikedByCaller { get; set; }
}