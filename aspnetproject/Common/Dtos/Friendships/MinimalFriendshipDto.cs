using aspnetproject.ProjectConstants.Enums;

namespace aspnetproject.Common.Dtos.Friendships;

public class MinimalFriendshipDto
{
    public int              RequesterId { get; set; }
    public int              AddresseeId { get; set; }
    
    public FriendshipStatus Status { get; set; }
    public DateTime         SentAt { get; set; }
}