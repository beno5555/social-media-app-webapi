# Constants and Configuration

## 1. No magic numbers or strings in logic

Avoid:
```csharp
bool canEdit = DateTime.UtcNow - message.CreatedAt <= TimeSpan.FromMinutes(20);
```

Prefer:
```csharp
bool canEdit = DateTime.UtcNow - message.CreatedAt <= Constants.EditMessageWindow;
```

```csharp
public static class Constants
{
    public const int PostTitleMaxLength = 100;
    public const int DefaultPageSize    = 10;
    public const int MaxPageSize        = 100;

    public static readonly TimeSpan EditMessageWindow = TimeSpan.FromMinutes(20);
}
```

## 2. Every user-facing message comes from one class

Build messages from shared words. Use `Func` fields for parameterized messages.

```csharp
public static class ResponseMessages
{
    private const string Successfully = "Successfully";
    private const string NotFound     = "Not Found";

    public const string PostUpdated  = $"Post Updated {Successfully}";
    public const string PostNotFound = $"Post {NotFound}";

    public static readonly Func<DateTime, string> UsernameCanBeChangedAgainOn =
        allowedDate => $"Username can be changed again on {allowedDate:yyyy-MM-dd}";
}
```

## 3. Named keys go in nested static classes

```csharp
public static class RateLimitConfig
{
    public static class Policies
    {
        public const string CreatePost = "CreatePost";
        public const string ReadPosts  = "ReadPosts";
    }
}

[EnableRateLimiting(RateLimitConfig.Policies.CreatePost)]
```

## 4. Tunable values come from configuration

Values that may change without a rebuild (intervals, retention days, batch sizes, SMTP settings) are bound to options classes that have defaults.

```csharp
public class OldLogsCleanupConfiguration
{
    public TimeSpan Interval      { get; set; } = TimeSpan.FromDays(1);
    public int      RetentionDays { get; set; } = 90;
    public int      BatchSize     { get; set; } = 1000;
}

services.Configure<OldLogsCleanupConfiguration>(configuration.GetSection("CleanupConfiguration:OldLogs"));
```

```json
"CleanupConfiguration": { "OldLogs": { "Interval": "1.00:00:00", "RetentionDays": 90, "BatchSize": 1000 } }
```

## 5. Group long constant files

```csharp
#region Posts
public const string CreatePost = "CreatePost";
#endregion
```
