using System.Security.Principal;
using aspnetproject.Common.Dtos.Users;

namespace aspnetproject.Common.Dtos.Messages;

public class StandardMessageDto
{
    public int    Id      { get; set; }
    public string Content { get; set; } = string.Empty;

    public MinimalUserDto Sender     { get; set; } = null!;
    public int            ReceiverId { get; set; }

    public DateTime SentAt { get; set; }
    
    public bool IsRead { get; set; }
    public bool IsEdited { get; set; }

}