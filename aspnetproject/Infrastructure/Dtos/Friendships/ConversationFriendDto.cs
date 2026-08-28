using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Friendships;

public class ConversationFriendDto
{
    public MinimalUserDto Friend             { get; set; } = null!;
    
    public string   LastMessageContent  { get; set; } = string.Empty;
    public DateTime LastMessageSentAt   { get; set; }
    public int      LastMessageSenderId { get; set; }

    public int UnreadCount { get; set; }
}