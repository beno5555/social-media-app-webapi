using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Posts;

public class UpdatePostDto
{
    [MaxLength(Constants.PostTitleMaxLength)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(Constants.PostContentMaxLength)]
    public string Content { get; set; } = string.Empty;
}