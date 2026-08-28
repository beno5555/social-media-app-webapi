namespace aspnetproject.ProjectConstants;

public static class ConsoleMessages
{
    #region Friends
    
    public const string NoFriendsMessage         = "You do not have any friends :(";
    public const string NoPendingRequestsMessage = "You do not have any pending requests";
    public const string NoSentRequestsMessage    = "You do not have any sent requests to other users";
    public const string DeleteFriend             = "Remove user from friends";
    
    public static readonly Func<string, string> SearchResultsForUsernameMessage = usernameQuery =>
        $"Search results for '{usernameQuery}':";
    public static readonly Func<string, string> NoSearchResultsForUsernameMessage = username =>
        $"No results for  '{username}'";
    
    #endregion
    
    #region Posts

    public const string FeedMessage          = "See what your friends have been up to!";
    public const string NoPostsInFeedMessage = "You're all caught up!";
    public const string OwnPostsMessage      = "See your posts";
    public const string NoOwnPosts           = "You do not have any posts";


    public static readonly Func<string, string> NoCommentsUnderPost = postTitle =>
        $"No Comments under post '{postTitle}'";

    
    #endregion

    #region ConsoleMessages
    
    public const string ConversationListMessage = "Your chats with your friends";
    public const string NoConversationsMessage  = "You do not have any conversations";
    public const string NewConversationMessage  = "New conversation - select a friend";

    public static readonly Func<string, string> ConversationLabel = friendUsername =>
        $"Conversation with '{friendUsername}'";
    #endregion

}
