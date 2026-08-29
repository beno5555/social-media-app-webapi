using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Comments;

public class EditCommentDto
{
    [MaxLength(Constants.PostContentMaxLength)]
    public string Content { get; set; } = string.Empty;
}