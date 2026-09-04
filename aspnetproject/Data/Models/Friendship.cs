using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Data.Models;

public class Friendship : BaseEntity
{
    public FriendshipStatus FriendshipStatus { get; set; } = FriendshipStatus.Pending;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public int   RequesterUserId { get; set; }
    public User? RequesterUser   { get; set; }

    public int   AddresseeUserId { get; set; }
    public User? AddresseeUser   { get; set; }
}