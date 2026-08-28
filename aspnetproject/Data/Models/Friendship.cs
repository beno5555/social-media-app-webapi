using System.ComponentModel.DataAnnotations.Schema;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Migrations;

namespace aspnetproject.Models;

public class Friendship
{
    public FriendshipStatus FriendshipStatus { get; set; } = FriendshipStatus.Pending;
    
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
    public DateTime SentAt        { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public int   RequesterUserId { get; set; }
    public User? RequesterUser   { get; set; }

    public int   AddresseeUserId { get; set; }
    public User? AddresseeUser   { get; set; }
}