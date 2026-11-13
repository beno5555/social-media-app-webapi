using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Posts;

// Sent as multipart/form-data. A post needs a title, and either text or at least one image.
public class CreatePostDto
{
    [MaxLength(Constants.PostTitleMaxLength)]
    [Required]
    public string PostTitle { get; set; } = string.Empty;

    [MaxLength(Constants.PostContentMaxLength)]
    public string? PostContent { get; set; }

    // In the order they should appear. Checked by content, not by name or declared type.
    public List<IFormFile> Images { get; set; } = [];
}
