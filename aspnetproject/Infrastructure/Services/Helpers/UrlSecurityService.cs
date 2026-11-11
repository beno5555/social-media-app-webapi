namespace aspnetproject.Infrastructure.Services.Helpers;

// Guards the return address WebCity puts in emailed links. Trusts exactly the origins in Cors:AllowedOrigins,
// the same list that drives the cross-origin policy, so the two can never disagree.
public class UrlSecurityService
{
    private readonly HashSet<string> _allowedRedirectOrigins;

    public UrlSecurityService(IConfiguration configuration)
    {
        var configuredOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        _allowedRedirectOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var configuredOrigin in configuredOrigins)
        {
            bool isAbsoluteUrl = Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var originUri);
            if (isAbsoluteUrl)
            {
                _allowedRedirectOrigins.Add(ToOrigin(originUri!));
            }
        }
    }

    public bool IsAllowedRedirect(string redirectUrl)
    {
        bool isAllowed = false;

        bool isAbsoluteUrl = Uri.TryCreate(redirectUrl, UriKind.Absolute, out var redirectUri);
        if (isAbsoluteUrl)
        {
            isAllowed = _allowedRedirectOrigins.Contains(ToOrigin(redirectUri!));
        }

        return isAllowed;
    }

    // Scheme and authority (host plus any non-default port), so a trusted host on another port or scheme does not match.
    private static string ToOrigin(Uri uri)
    {
        return $"{uri.Scheme}://{uri.Authority}";
    }
}
