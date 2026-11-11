# C# / .NET

## 1. Project settings and structure

Enable nullable reference types, implicit usings and file-scoped namespaces. Put each top-level type (class, record, enum, interface, struct) in its own file named after it; see [layering.md](layering.md) §6 for the exceptions. Use explicit constructors that assign `private readonly` fields. Use primary constructors only if the codebase already does.

```csharp
namespace aspnetproject.Infrastructure.Services.BusinessLogic.Content;

public class CommentService : BaseService
{
    private readonly CommentRepository _commentRepository;

    public CommentService(CommentRepository commentRepository, DatabaseLogger dbLogger) : base(dbLogger)
    {
        _commentRepository = commentRepository;
    }
}
```

## 2. `var` vs explicit types

Use `var` when the right-hand side makes the type obvious. Use explicit types for primitives and booleans you want to stand out.

```csharp
var  response = new ApplicationResponse<SentMessageDto>();
int  userId   = GetUserId();
bool isAdmin  = IsAdministrator();
```

## 3. Pattern matching

```csharp
if (post is not null) { ... }
catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 }) { ... }
```

## 4. Null-forgiving `!`

Use `!` only where an earlier check or framework guarantee makes the value non-null. It does not prevent a runtime null.

Allowed (authorized endpoint, so the claim exists):
```csharp
User.FindFirst(ClaimTypes.NameIdentifier)!.Value
```

Avoid (navigation never loaded):
```csharp
post.User!.Username
```

## 5. Braces and wrapping

Use Allman braces. When a method has more than three parameters, put one per line and the closing `)` on its own line.

```csharp
public PostService(
    PostRepository       postRepository,
    FriendshipRepository friendshipRepository,
    DatabaseLogger       dbLogger
    ) : base(dbLogger)
{
    ...
}
```

## 6. Vertical alignment

In consecutive declarations, align types, names and `=` into columns.

```csharp
private readonly PostRepository       _postRepository;
private readonly FriendshipRepository _friendshipRepository;

public string   Username    { get; set; } = string.Empty;
public DateTime DateOfBirth { get; set; }

var messageToAdd = MessageMapper.ToEntity(senderId, receiverId, createMessageDto);
var message      = await _messageRepository.AddMessageAsync(messageToAdd);
```

## 7. Blank lines separate the phases of a method

The phases are setup, fetch, branch and return. See the reference service in section 10.

## 8. Background work

Use one DI scope per tick and catch each cycle's exceptions so one failure does not stop the service. Honor cancellation.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(_interval);

    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        using var scope = _scopeFactory.CreateScope();
        try
        {
            await RunCycleAsync(scope.ServiceProvider, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            await LogResultAsync(scope.ServiceProvider.GetRequiredService<DatabaseLogger>(), false, null, ex.Message);
        }
    }
}
```

## 9. Concurrency

Singletons use thread-safe collections. Concurrent file writes are guarded.

```csharp
private readonly ConcurrentDictionary<int, int> _counts = new();
private static readonly SemaphoreSlim _fileLock = new(1, 1);
```

## 10. Reference service method

```csharp
public async Task<ApplicationResponse<FullPostDisplayDto>> UpdatePostAsync(int id, int callerId, UpdatePostDto updatePostDto)
{
    var response = new ApplicationResponse<FullPostDisplayDto>();
    var post     = await _postRepository.GetPostByIdAsync(id);

    if (post is not null)
    {
        bool belongsToCaller = post.UserId == callerId;
        if (belongsToCaller)
        {
            post.PostTitle     = updatePostDto.Title;
            post.PostContent   = updatePostDto.Content;
            post.LastUpdatedAt = DateTime.UtcNow;

            await _postRepository.SaveChangesAsync();

            response.Ok(PostMapper.ToFullDisplay(post), ResponseMessages.PostUpdated);
            await LogResultAsync(response.Succeeded, nameof(UpdatePostAsync), nameof(Post), null, id);
        }
        else
        {
            response.Fail(ResponseMessages.PostNotFound);
            await LogResultAsync(response.Succeeded, nameof(UpdatePostAsync), nameof(Post), "Caller does not own the post", id);
        }
    }
    else
    {
        response.Fail(ResponseMessages.PostNotFound);
        await LogResultAsync(response.Succeeded, nameof(UpdatePostAsync), nameof(Post), response.Message, id);
    }

    return response;
}
```

## 11. Reference controller action

```csharp
[HttpPut]
[Route("{id:int}")]
[EnableRateLimiting(RateLimitConfig.Policies.UpdatePost)]
public async Task<ActionResult<ApplicationResponse<FullPostDisplayDto>>> UpdatePost(int id, [FromBody] UpdatePostDto updatePostDto)
{
    int userId   = GetUserId();
    var response = await _postService.UpdatePostAsync(id, userId, updatePostDto);

    if (response.Succeeded)
    {
        return Ok(response);
    }

    return NotFound(response);
}
```
