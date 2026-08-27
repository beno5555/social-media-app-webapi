using aspnetproject.ProjectConstants.Enums;

namespace aspnetproject.Common.Dtos.Friendships;

public class MinimalFriendshipDto
{
    public int              RequesterId { get; set; }
    public int              AddresseeId { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime         SentAt { get; set; }
}