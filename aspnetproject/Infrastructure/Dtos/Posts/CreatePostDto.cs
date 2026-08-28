using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Posts;

public class CreatePostDto
{
    [MaxLength(Constants.PostTitleMaxLength)]
    public string PostTitle   { get; set; } = string.Empty;
    
    [MaxLength(Constants.PostContentMaxLength)]
    public string PostContent { get; set; } = string.Empty;
}