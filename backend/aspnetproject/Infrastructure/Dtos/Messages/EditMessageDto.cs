using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Messages;

public class EditMessageDto
{
    [MaxLength(Constants.MessageMaxLength)]
    [Required]
    public string NewContent { get; set; } = string.Empty;
}