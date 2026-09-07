namespace aspnetproject.Infrastructure.Services.Helpers;

public static class UrlSecurityService
{
    private static readonly HashSet<string> AllowedRedirectOrigins = new(StringComparer.OrdinalIgnoreCase)
    {
        "http://localhost:5500",
    };

    public static bool IsAllowedRedirect(string redirectUrl)
    {
        bool isAllowed = false;
        if (Uri.TryCreate(redirectUrl, UriKind.Absolute, out var uri))
        {
            var origin = $"{uri.Scheme}://{uri.Authority}";
            isAllowed = AllowedRedirectOrigins.Contains(origin);
        }
        
        return isAllowed;
    }
}