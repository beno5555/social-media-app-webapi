namespace aspnetproject.Infrastructure.Dtos.Users.Friends;

public class DisplayFriendDto
{
    public int       Id           { get; set; }
    public string    Username     { get; set; } = string.Empty;
    public DateTime? LastActiveAt { get; set; }
    public bool      IsActive     => LastActiveAt == null;
}