# Control Flow

## 1. Single exit outside the boundary layer

Services, repositories, mappers and helpers have one `return`, at the end. Nest the success path inside the positive condition, and put each failure in its matching `else`.

Avoid:
```csharp
var post = await _postRepository.GetPostByIdAsync(id);
if (post is null)
{
    return Fail(ResponseMessages.PostNotFound);
}
return Ok(PostMapper.ToFullDisplay(post));
```

Prefer:
```csharp
var response = new ApplicationResponse<FullPostDisplayDto>();
var post     = await _postRepository.GetPostByIdAsync(id);

if (post is not null)
{
    response.Ok(PostMapper.ToFullDisplay(post), ResponseMessages.PostRetrieved);
}
else
{
    response.Fail(ResponseMessages.PostNotFound);
}

return response;
```

## 2. Name conditions before branching

Avoid:
```csharp
if (post.UserId == callerId || User.IsInRole("Administrator")) { ... }
```

Prefer:
```csharp
bool belongsToCaller = post.UserId == callerId;
if (belongsToCaller || isAdmin) { ... }
```

## 3. Accumulate one result, even for plain return types

```csharp
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
```

## 4. Extract a helper when nesting gets deep

When nesting goes beyond about three levels, move a block into a private helper that returns its own result. Do not switch to early returns.

```csharp
var friendshipCheck = await ValidateFriendshipAsync(senderId, receiverId);

if (friendshipCheck.Succeeded)
{
    // send the message
}
else
{
    response.Fail(friendshipCheck.Message);
}
```

## 5. Boundary layer exception: controllers and handlers

Controllers, route handlers and hub endpoints may return early. Check the success case first and let failure fall through.

```csharp
var response = await _postService.GetPostByIdAsync(id);

if (response.Succeeded)
{
    return Ok(response);
}

return NotFound(response);
```

## 6. Always use braces

Avoid:
```csharp
if (wasLastConnection) await _presenceService.NotifyUserOfflineAsync(userId);
```

Prefer:
```csharp
if (wasLastConnection)
{
    await _presenceService.NotifyUserOfflineAsync(userId);
}
```
