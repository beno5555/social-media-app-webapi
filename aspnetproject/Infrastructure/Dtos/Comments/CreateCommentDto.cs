using System.ComponentModel.DataAnnotations;
using aspnetproject.ProjectConstants;

namespace aspnetproject.Common.Dtos.Comments;
    
public class CreateCommentDto
{
    [MaxLength(Constants.CommentMaxLength)]
    public string Content  { get; set; } = string.Empty;
    
    public int      PostId        { get; set; }
}