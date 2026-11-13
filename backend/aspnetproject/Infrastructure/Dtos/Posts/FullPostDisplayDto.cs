using aspnetproject.Infrastructure.Dtos.Comments;
using aspnetproject.Infrastructure.Dtos.Images;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Posts;

public class FullPostDisplayDto
{
    public int    Id    { get; set; }
    public string Title { get; set; } = string.Empty;

    public MinimalUserDto Author { get; set; } = null!;
    
    public string   PostContent { get; set; } = string.Empty;
    public DateTime UploadedAt  { get; set; }

    // In upload order.
    public List<ImageDto> Images { get; set; } = [];

    public List<StandardCommentDto> Comments { get; set; } = [];
}