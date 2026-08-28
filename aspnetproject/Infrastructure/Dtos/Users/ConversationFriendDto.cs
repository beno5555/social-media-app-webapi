namespace aspnetproject.Common.Dtos.Users;

public class ConversationFriendDto
{
    public MinimalUserDto Friend             { get; set; } = null!;
    
    public string   LastMessageContent  { get; set; } = string.Empty;
    public DateTime LastMessageSentAt   { get; set; }
    public int      LastMessageSenderId { get; set; }

    public int UnreadCount { get; set; }
}