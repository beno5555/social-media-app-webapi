using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Messages;

// Sent as multipart/form-data. A message needs text or at least one image.
public class CreateMessageDto
{
    [MaxLength(Constants.MessageMaxLength)]
    public string? Content { get; set; }

    // In the order they should appear. Checked by content, not by name or declared type.
    public List<IFormFile> Images { get; set; } = [];
}
