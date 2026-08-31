using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Messages;

public class CreateMessageDto
{
    [MaxLength(Constants.MessageMaxLength)]
    [Required]
    public string Content { get; set; } = string.Empty;
}