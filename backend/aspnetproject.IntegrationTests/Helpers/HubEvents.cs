namespace aspnetproject.IntegrationTests.Helpers;

// The events MessageHub pushes to clients. HubTestClient records every event in All from the moment it connects,
// so a push that arrives before the test starts waiting is not lost. Add new server events here.
public static class HubEvents
{
    public const string ReceiveMessage = "ReceiveMessage";
    public const string MessageSeen    = "MessageSeen";
    public const string MessageEdited  = "MessageEdited";
    public const string ReceiveComment = "ReceiveComment";
    public const string UserOnline     = "UserOnline";
    public const string UserOffline    = "UserOffline";

    public static readonly string[] All =
    [
        ReceiveMessage,
        MessageSeen,
        MessageEdited,
        ReceiveComment,
        UserOnline,
        UserOffline
    ];
}
