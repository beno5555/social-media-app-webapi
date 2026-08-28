using System.ComponentModel.DataAnnotations;
using aspnetproject.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Messages;

public class CreateMessageDto
{

    [MaxLength(Constants.MessageMaxLength)]
    public string MessageContent { get; set; } = string.Empty;
}