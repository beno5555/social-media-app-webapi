namespace aspnetproject.Common.ProjectConstants;

public static class ResponseMessages
{
    #region Words

    private const string Successfully = "Successfully";
    private const string Successful   = "Successful";
    private const string CouldNot     = "Could not";
    private const string Exists       = "already exists";
    private const string NotFound     = "Not Found";
    private const string IsRequired   = "Is Required";
    
    #region Verbs

    private const string Created   = "Created";
    private const string Retrieved = "Retrieved";
    private const string Fetched   = "Fetched";
    
    private const string Update  = "Update";
    private const string Updated = "Updated";
    
    private const string Edit   = "Edit";
    private const string Edited = "Edited";
    
    private const string Delete  = "Delete";
    private const string Deleted = "Deleted";
    
    private const string Removed  = "Removed";
    private const string Uploaded = "Uploaded";
    
    private const string Send = "Send";
    private const string Sent = "Sent";

    private const string Login        = "Login";
    private const string Registration = "Registration";
    
    #endregion
    
    #endregion
    
    #region Accounts
    
    public static readonly Func<string, string> SearchResultsForUsername = usernameQuery => $"Search results for '{usernameQuery}':";

    public static readonly Func<string, string> NoAccountsMatchingUsername =
        usernameQuery => $"No Accounts matching '{usernameQuery}':";

    public static readonly Func<DateTime, string> UsernameCanBeChangedAgainOn = allowedChangeDate =>
        $"Username can be changed again on {allowedChangeDate:yyyy-MM-dd}";
    
    public const string AccountCreated   = $"Account {Created} {Successfully}";
    public const string AccountRetrieved = $"Account {Retrieved} {Successfully}";
    public const string AccountDeleted   = $"Account {Deleted} {Successfully}";

    public const string UserWithEmailExists    = $"User with email {Exists}";
    public const string UserWithUsernameExists = $"User with username {Exists}";
    
    public const string UserNotFound = $"User {NotFound}";
    
    public const string ProfileRetrieved = $"Profile {Retrieved} {Successfully}";
    public const string ProfileEdited    = $"Profile {Edited} {Successfully}";
    
    #endregion
    
    #region Auth

    public const string RegistrationSuccessful = $"{Registration} {Successful}";
    public const string UsernameIsAlreadyTaken = $"Username is already taken";
    public const string EmailIsAlreadyTaken    = $"Email is already taken";

    public const  string LoginSuccessful     = $"{Login} {Successful}";
    public const  string LoginErrorMessage   = "Invalid Username or Password";
    public const string RefreshErrorMessage = "Invalid or expired refresh token";
    
    #endregion
    
    #region Comments
    
    public const string CommentRetrieved = $"Comment {Retrieved} {Successfully}";
    public const string CommentUploaded  = $"Comment {Uploaded} {Successfully}";
    public const string CommentUpdated   = $"Comment {Updated} {Successfully}";
    
    public const string CommentDeletedSuccessfully = $"Comment {Deleted} {Successfully}";
    public const string CouldNotDeleteComment      = $"{CouldNot} {Deleted} Comment";
    public const string CommentNotFound            = $"Comment {NotFound}";

    public const string UserCommentsRetrieved = $"User Comments {Retrieved} {Successfully}";

    #endregion

    #region Friendships

    public const string FriendRequestSent                  = $"Friend Request {Sent} {Successfully}";
    public const string AddresseeNotFound                  = $"Addressee {NotFound}";
    public const string FriendRequestCannotBeSentToOneself = $"Friend Request {CouldNot} be {Sent} to oneself";

    public const string AlreadyFriends              = "You are already friends with this user";
    public const string PendingRequestAlreadyExists = "A pending friend request already exists";
    
    public const string ResponseSent           = $"Response {Sent} {Successfully}";
    public const string PendingRequestNotFound = $"Pending request {NotFound}";
    
    public const string RelationshipRemoved = $"Relationship {Removed} {Successfully}";
    public const string FriendshipNotFound  = $"Friendship {NotFound}";
    
    public const string RelationshipFetched  = $"Relationship {Fetched} {Successfully}";
    public const string RelationshipNotFound = $"Relationship {NotFound}";
    
    #endregion
    
    #region Messages
    
    public const string MessageSent     = $"Message {Sent} {Successfully}";
    public const string MessageEdited   = $"Message {Edited} {Successfully}";
    public const string MessageDeleted  = $"Message {Deleted} {Successfully}";
    public const string MessageNotFound = $"Message {NotFound} {Successfully}";
    
    public const string FriendsWithNoConversationRetrieved = $"Friends with no Conversation {Retrieved} {Successfully}";
    public const string CanOnlySendMessagesToFriends       = $"You can only {Send} Messages to your Friends";
    public const string CannotSendMessagesToOneself        = $"Cannot {Send} Messages to oneself";

    public const string ReceiverUserNotFound = $"Receiver {NotFound}";
    public const string EditWindowExpired    = $"Message {Edit} window has expired";

    public const string DoNotHavePermissionToEditMessage = $"You do not have Permission to {Edit} this Message";
    public const string DeleteRequestDeclined            = $"{Delete} Request Declined";
    
    #endregion

    #region Posts
    
    public const string PostNotFound  = $"Post {NotFound}";
    public const string PostRetrieved = $"Post {Retrieved} {Successfully}";
    public const string PostUploaded  = $"Post {Uploaded} {Successfully}";
    public const string PostUpdated   = $"Post {Updated} {Successfully}";
    public const string PostDeleted   = $"Post {Deleted} {Successfully}";

    public const string ContentRequired = $"Post Content {IsRequired}";
    public const string TitleRequired   = $"Post Title {IsRequired}";

    public const string CouldNotUpdatePost = $"{CouldNot} {Update} Post";
    public const string CouldNotDeletePost = $"{CouldNot} {Delete} Post";

    public const string Feed = "See what your friends have been up to!";
    public const string UserPostsRetrieved = $"User Posts{Retrieved} {Successfully}";

    #endregion

    #region Success Messages
    public const string ConversationFriendsListSuccessMessage = "Friends with whom you have conversations retrieved successfully";
    public const string ConversationRetrievedSuccessfully = $"Conversation retrieved successfully";
    
    public const string PostCommentsRetrieved = $"Post Comments {Retrieved} {Successfully}";

    public static readonly Func<string, string> ResourceRetrieved =
        resource => $"{resource} {Retrieved} {Successfully}";
    
    #endregion
    
    #region Failure Messages

    public const string InvalidRequest = "Invalid Request";

    #endregion
}