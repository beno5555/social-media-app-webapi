using aspnetproject.Common.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Messages;

public class StandardMessageDto
{
    public int    Id      { get; set; }
    public string Content { get; set; } = string.Empty;

    public MinimalUserDto Sender     { get; set; } = null!;
    public int            ReceiverId { get; set; }

    public DateTime SentAt { get; set; }
    
    public bool      Seen     { get; set; }
    public DateTime? SeenAt   { get; set; } = null;
    public bool      IsEdited { get; set; }

}