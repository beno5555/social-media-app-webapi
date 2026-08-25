using System.ComponentModel.DataAnnotations;
using aspnetproject.ProjectConstants;

namespace aspnetproject.BusinessLogic.Dtos.Posts;

public class UpdatePostDto
{
    [MaxLength(Constants.PostTitleMaxLength)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(Constants.PostContentMaxLength)]
    public string Content { get; set; } = string.Empty;
}