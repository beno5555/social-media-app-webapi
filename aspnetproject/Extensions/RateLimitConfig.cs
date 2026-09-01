using System.Security.Claims;
using System.Threading.RateLimiting;

namespace aspnetproject.Extensions;

public static class RateLimitConfig
{
    public static class Policies
    {
        #region Auth
        public const string Register             = "Register";
        public const string Login                = "Login";
        public const string Refresh              = "Refresh";
        public const string RequestPasswordReset = "RequestPasswordReset";
        public const string ResetPassword        = "ResetPassword";
        #endregion

        #region AccountManagement
        public const string DeleteOwnAccount       = "DeleteOwnAccount";
        public const string AdminDeleteAccount     = "AdminDeleteAccount";
        public const string DeactivateOwnAccount   = "DeactivateOwnAccount";
        public const string AdminDeactivateAccount = "AdminDeactivateAccount";
        public const string AdminActivateAccount   = "AdminActivateAccount";
        public const string RequestActivation      = "RequestActivation";
        public const string ConfirmActivation      = "ConfirmActivation";
        #endregion

        #region Comments
        public const string CreateComment     = "CreateComment";
        public const string GetOwnComments    = "GetOwnComments";
        public const string GetCommentsByPost = "GetCommentsByPost";
        public const string GetCommentById    = "GetCommentById";
        public const string EditComment       = "EditComment";
        public const string DeleteComment     = "DeleteComment";
        #endregion

        #region Friendships
        public const string SendFriendRequest    = "SendFriendRequest";
        public const string ReadFriendships      = "ReadFriendships";
        public const string RespondFriendRequest = "RespondFriendRequest";
        public const string RemoveFriend         = "RemoveFriend";
        #endregion

        #region Messages
        public const string SendMessage            = "SendMessage";
        public const string ReadMessages           = "ReadMessages";
        public const string MarkConversationAsRead = "MarkConversationAsRead";
        public const string EditMessage            = "EditMessage";
        public const string DeleteMessage          = "DeleteMessage";
        #endregion

        #region Posts
        public const string CreatePost = "CreatePost";
        public const string ReadPosts  = "ReadPosts";
        public const string UpdatePost = "UpdatePost";
        public const string DeletePost = "DeletePost";
        #endregion

        #region Users
        public const string AdminCreateUser  = "AdminCreateUser";
        public const string ReadUserProfile  = "ReadUserProfile";
        public const string SearchUsers      = "SearchUsers";
        public const string UpdateOwnProfile = "UpdateOwnProfile";
        public const string AdminUpdateUser  = "AdminUpdateUser";
        #endregion
    }

    public static void AddApplicationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            #region Auth (IP-based, pre-authentication)
            options.AddPolicy(Policies.Login, httpContext =>
                PerIpSlidingWindow(httpContext, 10, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.Register, httpContext =>
                PerIpSlidingWindow(httpContext, 5, TimeSpan.FromMinutes(60)));

            options.AddPolicy(Policies.Refresh, httpContext =>
                PerIpSlidingWindow(httpContext, 5, TimeSpan.FromMinutes(10)));

            options.AddPolicy(Policies.RequestPasswordReset, httpContext =>
                PerIpSlidingWindow(httpContext, 3, TimeSpan.FromMinutes(15)));

            options.AddPolicy(Policies.ResetPassword, httpContext =>
                PerIpSlidingWindow(httpContext, 5, TimeSpan.FromMinutes(15)));
            #endregion

            #region Account Management 
            options.AddPolicy(Policies.DeleteOwnAccount, httpContext =>
                PerUserSlidingWindow(httpContext, 3, TimeSpan.FromHours(24), segments: 4));

            options.AddPolicy(Policies.AdminDeleteAccount, httpContext =>
                PerUserSlidingWindow(httpContext, 30, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.DeactivateOwnAccount, httpContext =>
                PerUserSlidingWindow(httpContext, 5, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.AdminDeactivateAccount, httpContext =>
                PerUserSlidingWindow(httpContext, 30, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.AdminActivateAccount, httpContext =>
                PerUserSlidingWindow(httpContext, 30, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.RequestActivation, httpContext =>
                PerIpSlidingWindow(httpContext, 3, TimeSpan.FromMinutes(15)));

            options.AddPolicy(Policies.ConfirmActivation, httpContext =>
                PerIpSlidingWindow(httpContext, 5, TimeSpan.FromMinutes(15)));
            #endregion

            #region Comments
            options.AddPolicy(Policies.CreateComment, httpContext =>
                PerUserSlidingWindow(httpContext, 20, TimeSpan.FromMinutes(10)));

            options.AddPolicy(Policies.GetOwnComments, httpContext =>
                PerUserSlidingWindow(httpContext, 120, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.GetCommentsByPost, httpContext =>
                PerUserSlidingWindow(httpContext, 120, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.GetCommentById, httpContext =>
                PerUserSlidingWindow(httpContext, 120, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.EditComment, httpContext =>
                PerUserSlidingWindow(httpContext, 20, TimeSpan.FromMinutes(10)));

            options.AddPolicy(Policies.DeleteComment, httpContext =>
                PerUserSlidingWindow(httpContext, 20, TimeSpan.FromMinutes(10)));
            #endregion

            #region Friendships
            options.AddPolicy(Policies.SendFriendRequest, httpContext =>
                PerUserSlidingWindow(httpContext, 30, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.ReadFriendships, httpContext =>
                PerUserSlidingWindow(httpContext, 120, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.RespondFriendRequest, httpContext =>
                PerUserSlidingWindow(httpContext, 60, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.RemoveFriend, httpContext =>
                PerUserSlidingWindow(httpContext, 30, TimeSpan.FromHours(1)));
            #endregion

            #region Messages 
            options.AddPolicy(Policies.SendMessage, httpContext =>
                PerUserSlidingWindow(httpContext, 60, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.ReadMessages, httpContext =>
                PerUserSlidingWindow(httpContext, 120, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.MarkConversationAsRead, httpContext =>
                PerUserSlidingWindow(httpContext, 120, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.EditMessage, httpContext =>
                PerUserSlidingWindow(httpContext, 30, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.DeleteMessage, httpContext =>
                PerUserSlidingWindow(httpContext, 30, TimeSpan.FromMinutes(1)));
            #endregion

            #region Posts
            options.AddPolicy(Policies.CreatePost, httpContext =>
                PerUserSlidingWindow(httpContext, 10, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.ReadPosts, httpContext =>
                PerUserSlidingWindow(httpContext, 120, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.UpdatePost, httpContext =>
                PerUserSlidingWindow(httpContext, 20, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.DeletePost, httpContext =>
                PerUserSlidingWindow(httpContext, 20, TimeSpan.FromHours(1)));
            #endregion

            #region Users
            options.AddPolicy(Policies.AdminCreateUser, httpContext =>
                PerUserSlidingWindow(httpContext, 20, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.ReadUserProfile, httpContext =>
                PerUserSlidingWindow(httpContext, 120, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.SearchUsers, httpContext =>
                PerUserSlidingWindow(httpContext, 30, TimeSpan.FromMinutes(1)));

            options.AddPolicy(Policies.UpdateOwnProfile, httpContext =>
                PerUserSlidingWindow(httpContext, 10, TimeSpan.FromHours(1)));

            options.AddPolicy(Policies.AdminUpdateUser, httpContext =>
                PerUserSlidingWindow(httpContext, 30, TimeSpan.FromHours(1)));
            #endregion
            
            #region Global

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                PerUserSlidingWindow(httpContext, 300, TimeSpan.FromMinutes(1)));
            #endregion
        });
    }

    private static RateLimitPartition<string> PerIpSlidingWindow(HttpContext httpContext, int permitLimit, TimeSpan window)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var windowLimiter = RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            SegmentsPerWindow = 3,
            QueueLimit = 0
        });

        return windowLimiter;
    }

    private static RateLimitPartition<string> PerUserSlidingWindow(HttpContext httpContext, int permitLimit, TimeSpan window, int segments = 4)
    {
        var partitionKey = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        var windowLimiter = RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            SegmentsPerWindow = segments,
            QueueLimit = 0
        });

        return windowLimiter;
    }
}