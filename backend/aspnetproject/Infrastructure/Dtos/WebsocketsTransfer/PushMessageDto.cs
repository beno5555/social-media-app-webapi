using aspnetproject.Infrastructure.Dtos.Images;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.WebsocketsTransfer;

public class PushMessageDto
{
    public int    Id      { get; set; }
    public string Content { get; set; } = string.Empty;

    public List<ImageDto> Images { get; set; } = [];

    public MinimalUserDto Sender { get; set; } = null!;
    public DateTime       SentAt { get; set; }
}
