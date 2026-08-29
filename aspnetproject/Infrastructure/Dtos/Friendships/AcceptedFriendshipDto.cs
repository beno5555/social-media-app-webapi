using aspnetproject.Infrastructure.Dtos.Users.Friends;

namespace aspnetproject.Infrastructure.Dtos.Friendships;

public class AcceptedFriendshipDto
{
    public DisplayFriendDto OtherUser     { get; set; } = null!;
    public string           Status        { get; set; } = string.Empty;
    public DateTime         SentAt        { get; set; }
    public DateTime?        LastUpdatedAt { get; set; }
}