using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Posts;

public class CreatePostDto
{
    [MaxLength(Constants.PostTitleMaxLength)]
    [Required]
    public string PostTitle   { get; set; } = string.Empty;
    
    [MaxLength(Constants.PostContentMaxLength)]
    [Required]
    public string PostContent { get; set; } = string.Empty;
}