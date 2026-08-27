using System.ComponentModel.DataAnnotations;
using aspnetproject.ProjectConstants;

namespace aspnetproject.Common.Dtos.Messages;

public class EditMessageDto
{
    [MaxLength(Constants.MessageMaxLength)]
    public string NewContent { get; set; } = string.Empty;
}