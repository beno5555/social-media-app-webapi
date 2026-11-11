# Layering

Dependencies point one way: transport → services → repositories → database.

| Layer | Owns | Must not |
|---|---|---|
| Transport (controllers, handlers, hubs) | Binding input, reading the caller from claims, calling one service, mapping to HTTP | Business rules, DB access, outcome logging |
| Services | Business validation, ownership checks, orchestration, transactions, logging, notifications | HTTP status codes |
| Repositories | Queries, includes, paging, bulk operations, `SaveChanges` | Business rules, returning DTOs |
| Mappers | Pure entity ↔ DTO conversion | I/O, state |
| DTOs / queries | Shapes and declarative validation | Behavior |
| Common | Constants, messages, enums, result wrappers, attributes | Depending on upper layers |

## 1. Thin controllers

Avoid (business rule and data access in the controller):
```csharp
var post = await _dbContext.Posts.FindAsync(id);
if (post.UserId != GetUserId()) { return Forbid(); }
_dbContext.Posts.Remove(post);
```

Prefer:
```csharp
int  userId  = GetUserId();
bool isAdmin = IsAdministrator();

var response = await _postService.DeletePostAsync(userId, isAdmin, id);
```

## 2. Shared transport helpers go in a base class

```csharp
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected int  GetUserId()       => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    protected bool IsAdministrator() => User.IsInRole(nameof(RoleName.Administrator));
}
```

## 3. Mutate and notify in one service method

Hubs are push-only transport and do no validation.

```csharp
var message = await _messageRepository.AddMessageAsync(messageToAdd);
await _hubContext.Clients.Group(receiverId.ToString()).SendAsync("ReceiveMessage", MessageMapper.ToPush(message));
```

## 4. No speculative abstractions

Use an interface only when a second implementation exists or the user asks for one. Follow the codebase's choice.

Avoid:
```csharp
public interface IPostService { ... }   // single implementation, no test double needed
services.AddScoped<IPostService, PostService>();
```

Prefer:
```csharp
services.AddScoped<PostService>();
```

## 5. Base classes only for genuinely shared behavior

Good uses are `BaseService` (logging helper), `BaseRepository<T>` (query helpers) and `PeriodicHostedService` (timer loop). Do not use inheritance to share code between unrelated classes.

```csharp
public class PostService : BaseService
{
    public PostService(PostRepository postRepository, DatabaseLogger dbLogger) : base(dbLogger) { ... }
}
```

## 6. Group folders by domain area, mirrored across layers

```
Controllers/Users/FriendshipController.cs
Infrastructure/Services/BusinessLogic/Users/FriendshipService.cs
Infrastructure/Dtos/Friendships/StandardFriendshipDto.cs
```

One type per file, and the file is named after the type. Never collect a layer's types into one file (`Repositories.cs`, `Dtos.cs`, `Entities.cs`, `Enums.cs`, `Options.cs`).

Avoid:
```
Repositories/Repositories.cs        // UserRepository, PostRepository, CommentRepository…
```

Prefer:
```
Repositories/UserRepository.cs
Repositories/PostRepository.cs
Repositories/CommentRepository.cs
```

Exceptions: nested types stay inside their parent, and the response wrapper variants (`ApplicationResponse`, `ApplicationResponse<T>`, `ListResponse<T>`) may share `ApplicationResponse.cs`.

## 7. Compose startup from extension methods

```csharp
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationRateLimiting();
builder.Services.AddApplicationServices(builder.Configuration);
```

Lifetimes:
- Scoped for repositories and services.
- Singleton for in-memory trackers. They must use thread-safe collections.
- Transient for stateless senders.
