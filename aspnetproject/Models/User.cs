namespace aspnetproject.Models;

public class User : BaseEntity
{
    public string   Username     { get; set; } = string.Empty;
    public string   Email        { get; set; } = string.Empty;
    public DateTime DateOfBirth  { get; set; }  
    public string   PasswordHash { get; set; } = string.Empty;
    public string   PasswordSalt { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public ICollection<Log> Logs { get; set; } = [];
    // role

    public ICollection<Post>       Posts                  { get; set; } = [];
    public ICollection<Comment>    Comments               { get; set; } = [];
    public ICollection<Message>    SentMessages           { get; set; } = [];
    public ICollection<Message>    ReceivedMessages       { get; set; } = [];
    public ICollection<Friendship> SentFriendRequests     { get; set; } = [];
    public ICollection<Friendship> ReceivedFriendRequests { get; set; } = [];
    
}