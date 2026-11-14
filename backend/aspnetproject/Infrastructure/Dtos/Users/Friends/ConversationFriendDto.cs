namespace aspnetproject.Infrastructure.Dtos.Users.Friends;

public class ConversationFriendDto
{
    public DisplayFriendDto ConversationFriend { get; set; } = null!;

    public string   LastMessageContent  { get; set; } = string.Empty;
    public DateTime LastMessageSentAt   { get; set; }
    public int      LastMessageSenderId { get; set; }

    // Lets the client show something for a last message that is a photo with no text.
    public int LastMessageImageCount { get; set; }

    public int UnreadCount { get; set; }
}