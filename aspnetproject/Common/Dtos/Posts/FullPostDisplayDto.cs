using aspnetproject.BusinessLogic.Dtos.CommentDtos;
using aspnetproject.Common.Dtos.Comments;

namespace aspnetproject.Common.Dtos.Posts;

public class FullPostDisplayDto
{
    public int    Id    { get; set; }
    public string Title { get; set; } = string.Empty;
    
    public int      UserId     { get; set; }
    public string   Username   { get; set; } = string.Empty;

    public string   PostContent { get; set; } = string.Empty;
    public DateTime UploadedAt  { get; set; }

    public List<DisplayCommentDto> Comments { get; set; } = [];
}