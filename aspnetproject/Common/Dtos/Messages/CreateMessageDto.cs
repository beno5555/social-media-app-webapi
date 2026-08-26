using System.ComponentModel.DataAnnotations;
using aspnetproject.ProjectConstants;

namespace aspnetproject.Common.Dtos.Messages;

public class CreateMessageDto
{
    public int ReceiverId { get; set; }

    [MaxLength(Constants.MessageMaxLength)]
    public string MessageContent { get; set; } = string.Empty;
}