using aspnetproject.Common.Dtos.Comments;
using aspnetproject.Common.Dtos.Users;
using aspnetproject.Infrastructure.Dtos.Comments;

namespace aspnetproject.Common.Dtos.Posts;

public class FullPostDisplayDto
{
    public int    Id    { get; set; }
    public string Title { get; set; } = string.Empty;

    public MinimalUserDto Author { get; set; } = null!;
    
    public string   PostContent { get; set; } = string.Empty;
    public DateTime UploadedAt  { get; set; }

    public List<StandardCommentDto> Comments { get; set; } = [];
}