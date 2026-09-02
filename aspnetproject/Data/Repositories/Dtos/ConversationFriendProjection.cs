namespace aspnetproject.Data.Repositories.Dtos;

public class ConversationFriendProjection
{
    public int       FriendId           { get; set; }
    public string    FriendUsername     { get; set; } = string.Empty;
    public DateTime? FriendLastActiveAt { get; set; }
    public bool      IsDeleted          { get; set; }
    public bool      IsDeactivated      { get; set; }

    public string   LastMessageContent  { get; set; } = string.Empty;
    public DateTime LastMessageSentAt   { get; set; }
    public int      LastMessageSenderId { get; set; }

    public int UnreadCount { get; set; }
}