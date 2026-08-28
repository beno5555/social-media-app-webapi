using System.ComponentModel.DataAnnotations;
using aspnetproject.ProjectConstants;

namespace aspnetproject.Common.Dtos.Posts;

public class CreatePostDto
{
    [MaxLength(Constants.PostTitleMaxLength)]
    public string PostTitle   { get; set; } = string.Empty;
    
    [MaxLength(Constants.PostContentMaxLength)]
    public string PostContent { get; set; } = string.Empty;
}