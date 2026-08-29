using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Data.Models;

public class Friendship
{
    public FriendshipStatus FriendshipStatus { get; set; } = FriendshipStatus.Pending;
    
    public DateTime  CreatedAt     { get; set; } = DateTime.UtcNow;
    public DateTime  SentAt        { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; } = null;

    public int   RequesterUserId { get; set; }
    public User? RequesterUser   { get; set; }

    public int   AddresseeUserId { get; set; }
    public User? AddresseeUser   { get; set; }
}