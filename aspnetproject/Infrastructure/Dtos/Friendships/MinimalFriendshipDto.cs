namespace aspnetproject.Infrastructure.Dtos.Friendships;

public class MinimalFriendshipDto
{
    public int              RequesterId { get; set; }
    public int              AddresseeId { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime         SentAt { get; set; }
}