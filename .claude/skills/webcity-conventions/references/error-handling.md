# Error Handling

## 1. Expected failures are values

Use a result wrapper with `Succeeded`, `Message` and `Data`, plus a separate list wrapper:

```csharp
public class ApplicationResponse
{
    public bool    Succeeded { get; set; } = true;
    public string? Message   { get; set; }

    public void Ok(string? message = null)            { Succeeded = true;  Message = message; }
    public virtual void Fail(string? message = null)  { Succeeded = false; Message = message; }
}

public class ApplicationResponse<T> : ApplicationResponse { public T? Data { get; set; } /* Ok(T data, ...) */ }
public class ListResponse<T>        : ApplicationResponse { public List<T> Data { get; set; } = []; }
```

Avoid:
```csharp
if (!userExists) { throw new NotFoundException("User not found"); }
```

Prefer:
```csharp
response.Fail(ResponseMessages.UserNotFound);
```

## 2. No wrapper when a method cannot fail

```csharp
public async Task LogoutAsync(string? refreshToken) { ... }      // nothing can fail
public async Task<int> GetUnreadConversationsCount(int userId)  // plain value
```

## 3. An empty list is a success

Avoid:
```csharp
if (posts.Count == 0) { response.Fail(ResponseMessages.PostNotFound); }
```

Prefer:
```csharp
response.Ok(postDtos, ResponseMessages.UserPostsRetrieved);   // may be []
```

## 4. Exceptions are for the unexpected, handled in one place

The global handler maps conflicts to 409 and everything else to 500. It returns full exception detail only in Development.

```csharp
if (exception is DbUpdateException)
{
    httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
}
else
{
    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
}
```

## 5. Narrow, known recoveries use exception filters

```csharp
catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
{
    logToAdd.UserId = null;   // FK target deleted meanwhile; keep the log anyway
    await _dbContext.SaveChangesAsync();
}
```

Requested cancellation is silent:
```csharp
catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
```

## 6. Map status codes per call site in the controller

- If every failure path of the service means the same thing, use that status, for example not found → 404.
- If the failure paths mean different things (not found, self-request, already exists), collapse them to 400.
- Never parse `Message` to choose a status.

Avoid:
```csharp
if (response.Message == ResponseMessages.AlreadyFriends) { return Conflict(response); }
```

Prefer:
```csharp
if (response.Succeeded)
{
    return Ok(response);
}

return BadRequest(response);   // SendRequest has several failure meanings
```

## 7. Created resources return 201 with a real location

```csharp
return CreatedAtAction(nameof(GetPostById), new { response.Data!.Id }, response);
```

The target must be an existing GET action, never the POST action itself.
