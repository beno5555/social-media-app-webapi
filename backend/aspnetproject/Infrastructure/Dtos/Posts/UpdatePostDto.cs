using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Posts;

public class UpdatePostDto
{
    [MaxLength(Constants.PostTitleMaxLength)]
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(Constants.PostContentMaxLength)]
    [Required]
    public string Content { get; set; } = string.Empty;
}