# Logging

## 1. Two separate channels

| Channel | Content | Written from |
|---|---|---|
| Structured action log (DB table) | Business outcomes: action, entity, entity id, succeeded, user id, details | Services, via a base-class helper |
| System/error log (file or sink) | Unhandled exceptions, infrastructure and logging failures | Global exception handler, loggers |

Action names follow `{ServiceName}.{MethodName}`:
```csharp
Action = $"{_serviceName}.{method}"   // _serviceName = GetType().Name in BaseService
```

## 2. No business logging in controllers

Avoid:
```csharp
public async Task<IActionResult> DeletePost(int id)
{
    _logger.LogInformation("Deleting post {Id}", id);
    ...
}
```

Prefer:
```csharp
// in PostService.DeletePostAsync
await LogResultAsync(response.Succeeded, nameof(DeletePostAsync), nameof(Post), null, id);
```

## 3. Decide what to log per method

| Case | Log |
|---|---|
| Any failure | Always |
| Data-changing success (create, update, delete, role change) | Yes |
| Security-relevant read (user search, enumeration-capable) | Yes |
| High-frequency, low-value read (feed, own inbox) | No |
| Success that only repeats the row just written (sent message) | No, log failure only |
| High-frequency toggle (like, unlike) | Failure only. The row itself is the record |
| Login | Failure only (brute-force signal) |

## 4. Set the result first, then log from it

```csharp
response.Fail(ResponseMessages.PostNotFound);
await LogResultAsync(response.Succeeded, nameof(UpdatePostAsync), nameof(Post), response.Message, id);
```

## 5. Loggers never throw into business logic

```csharp
public async Task LogUserActionAsync(CreateLogDto createLogDto)
{
    try
    {
        // build entity, save
    }
    catch (Exception ex)
    {
        await _systemLogger.LoggingErrorAsync(ex, $"Failed to write log entry {createLogDto.Action}");
    }
}
```

## 6. Background jobs log as system actions

There is no HttpContext in a background job, so the user id is null and the entry is not marked as an authorized request:
```csharp
await dbLogger.LogSystemActionAsync(log);
```

## 7. Never log secrets

Avoid:
```csharp
details: $"Reset requested with token {token} for {dto.Email} / {dto.Password}"
```

Prefer:
```csharp
details: "Password reset token issued"
```
