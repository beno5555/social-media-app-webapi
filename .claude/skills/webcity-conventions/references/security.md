# Security

## 1. Errors must not reveal what exists

Avoid:
```csharp
response.Fail(userExists ? "Wrong password" : "No account with that username");
```

Prefer:
```csharp
response.Fail(ResponseMessages.InvalidCredentials);   // same message for both cases
```

The same rule applies to refresh and logout: a missing, invalid, expired or revoked token all get one response (401). A resource owned by someone else returns the same failure as a missing one.

## 2. Scope ownership inside the check or query

Avoid (fetch, then authorize in a separate step, which also leaks existence):
```csharp
var post = await _postRepository.GetByIdAsync(id);
if (post.UserId != callerId) { return Forbid(); }
```

Prefer:
```csharp
await _dbSet.Where(message => message.Id == id && message.SenderUserId == callerId).ExecuteDeleteAsync();
```

## 3. Identity comes from claims, never from the body

Avoid:
```csharp
public class CreatePostDto { public int UserId { get; set; } ... }
```

Prefer:
```csharp
int userId = GetUserId();   // from the NameIdentifier claim
await _postService.UploadPostAsync(userId, createPostDto);
```

## 4. Hash secrets at rest; generate them with a cryptographic RNG

```csharp
var bytes = RandomNumberGenerator.GetBytes(64);
var token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
var hash  = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));   // store only this
```

Passwords use PBKDF2 or a similar slow KDF with a per-user salt.

## 5. Token transport

The refresh token goes in a hardened cookie and rotates on every use. The access token goes in the response body.

```csharp
Response.Cookies.Append("refreshToken", token, new CookieOptions
{
    HttpOnly = true,
    Secure   = true,
    SameSite = SameSiteMode.Strict,
    Expires  = DateTimeOffset.UtcNow.AddDays(refreshTokenDays)
});
```

Revoke every session on password reset, deactivation and deletion:
```csharp
await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
```

## 6. Allowlist redirect targets

Any URL a client supplies that ends up in an email or a redirect must match an allowlist. The allowlist comes from configuration, never a hardcoded set: WebCity reuses `Cors:AllowedOrigins`, so the trusted callers and the trusted link targets cannot disagree. Compare scheme and authority, ignoring case, and normalize the configured entries the same way as the incoming URL. An empty list rejects everything.

```csharp
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

private string ToOrigin(Uri uri)
{
    return $"{uri.Scheme}://{uri.Authority}";
}
```

## 7. Rate-limit every mutating endpoint and most reads

- Unauthenticated endpoints are limited per IP.
- Authenticated endpoints are limited per user.
- A global fallback limiter backs everything up.

```csharp
[EnableRateLimiting(RateLimitConfig.Policies.CreatePost)]
```

## 8. Authorize at controller level and opt out explicitly

```csharp
[Route("api/posts")]
[Authorize]
public class PostController : BaseController
{
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<...> GetPostById(int id) { ... }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(RoleName.Administrator))]   // enum name, not "Administrator"
    public async Task<...> AdminUpdate(int id, ...) { ... }
}
```

## 9. Validate twice, using the same constants

DTO:
```csharp
[MaxLength(Constants.PostTitleMaxLength)]
public string PostTitle { get; set; } = string.Empty;
```

DB:
```csharp
builder.Property(post => post.PostTitle).HasMaxLength(Constants.PostTitleMaxLength);
```
