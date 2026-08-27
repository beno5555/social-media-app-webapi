using aspnetproject.Common.Dtos.Users;
using aspnetproject.ProjectConstants.Enums;

namespace aspnetproject.Common.Dtos.Friendships;

public class StandardFriendshipDto
{
    public MinimalUserDto   OtherUser { get; set; } = null!;
    public string Status        { get; set; } = string.Empty;
    public DateTime         SentAt     { get; set; }
    public DateTime                LastUpdatedAt             { get; set; }
}