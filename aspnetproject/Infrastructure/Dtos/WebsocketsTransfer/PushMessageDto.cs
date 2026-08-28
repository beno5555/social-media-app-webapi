using aspnetproject.Common.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.WebsocketsTransfer;

public class PushMessageDto
{
    public int    Id      { get; set; }
    public string Content { get; set; } = string.Empty;

    public MinimalUserDto Sender { get; set; } = null!;
    public DateTime       SentAt { get; set; }
}