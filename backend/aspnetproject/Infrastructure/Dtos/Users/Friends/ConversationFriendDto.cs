namespace aspnetproject.Infrastructure.Dtos.Users.Friends;

public class ConversationFriendDto
{
    public DisplayFriendDto ConversationFriend { get; set; } = null!;

    public string   LastMessageContent  { get; set; } = string.Empty;
    public DateTime LastMessageSentAt   { get; set; }
    public int      LastMessageSenderId { get; set; }

    public int UnreadCount { get; set; }
}