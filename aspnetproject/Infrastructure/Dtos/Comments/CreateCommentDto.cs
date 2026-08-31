using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Comments;
    
public class CreateCommentDto
{
    [MaxLength(Constants.CommentMaxLength)]
    [Required]
    public string Content  { get; set; } = string.Empty;
}