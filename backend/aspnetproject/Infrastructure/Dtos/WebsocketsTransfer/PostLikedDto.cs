using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.WebsocketsTransfer;

public class PostLikedDto
{
    public int            PostId    { get; set; }
    public MinimalUserDto LikedBy   { get; set; } = null!;
    public int            LikeCount { get; set; }
}
