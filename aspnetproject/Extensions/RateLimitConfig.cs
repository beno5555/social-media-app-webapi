using System.Threading.RateLimiting;

namespace aspnetproject.Extensions;

public static class RateLimitConfig
{
    public static class Policies
    {
        public const string Login    = "Login";
        public const string Register = "Register";
        public const string Refresh  = "Refresh";
        
        public const string ForgotPassword = "ForgotPassword";
        public const string ResetPassword  = "ResetPassword";
    }

    public static void AddApplicationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Policies.Login, httpContext =>
                PerIpSlidingWindow(httpContext, 10, TimeSpan.FromMinutes(5)));
            
            options.AddPolicy(Policies.Register, httpContext =>
                PerIpSlidingWindow(httpContext, 5, TimeSpan.FromMinutes(60)));
            
            options.AddPolicy(Policies.Refresh, httpContext =>
                PerIpSlidingWindow(httpContext, 30, TimeSpan.FromMinutes(1)));
            
            options.AddPolicy(Policies.ForgotPassword, httpContext =>
                PerIpSlidingWindow(httpContext, 3, TimeSpan.FromMinutes(15)));
            
            options.AddPolicy(Policies.ResetPassword, httpContext =>
                PerIpSlidingWindow(httpContext, 10, TimeSpan.FromMinutes(15)));
            
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
}