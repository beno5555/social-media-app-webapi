# AspNetProject — Controllers

> Part of the AspNetProject doc set. See also: `core.md`, `data.md`, `auth.md`, `controllers.md`.

---

## Controllers Layer

### `BaseController`

Abstract, carries `[ApiController]` (inherited automatically by every derived controller — no need to repeat it) and `[Authorize]` is instead applied per-controller, not here, so controllers with mostly-public endpoints can invert it.

```csharp
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected int GetUserId()
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        return int.Parse(userIdRaw);
    }
}
```

`GetUserId()` reads the caller's own id from JWT claims. Note: claims are populated by `UseAuthentication` on every request regardless of whether the hit endpoint carries `[Authorize]` — `[Authorize]` only gates rejection of unauthenticated requests, it doesn't control whether `HttpContext.User` gets parsed.

Kept as a method, not a property — it does work (parses a claim) and can throw if called on an anonymous-allowed endpoint hit without a token; properties are conventionally expected to be cheap and non-throwing, so a method name is the more honest signal.

### `PageQuery`

Shared query-binding DTO for paginated list endpoints, in `/Common/Dtos/Common/`:

```csharp
public class PageQuery
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}
```

Bound via `[FromQuery]`. Properties are defaulted, deliberately: several service methods (e.g. `GetFeedAsync`) treat `null` page/size as "no pagination, return everything," a mode used by internal/non-API callers (cascade cleanup, seeding). Controllers unpack `PageQuery` into the individual `page`/`pageSize` primitives before calling a service — the service layer does not take a dependency on `PageQuery` itself. No size clamping/validation yet.

Reused as-is by `CommentController`'s paginated endpoints.

### `PostsController`

Route: `/posts`. Inherits `BaseController`. `[Authorize]` at the controller level, with `[AllowAnonymous]` on the public-read endpoints — most actions require auth, so this reads better than tagging most methods individually.

| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/posts/{id}` | Anonymous | Single post |
| GET | `/posts` | Anonymous | Paginated list |
| GET | `/posts/feed` | Required | Friends-only, via `PostService.GetFeedAsync` |
| GET | `/posts/mine` | Required | Caller's own posts, `userId` from JWT |
| GET | `/posts/user/{userId}` | Anonymous | Any user's posts — public-profile-style visibility, mirrors platforms where post history is public even to logged-out viewers; not gated by friendship |
| POST | `/posts` | Required | Create |
| PUT | `/posts/{id}` | Required | Update, ownership-scoped |
| DELETE | `/posts/{id}` | Required | Delete, ownership-scoped |

Route ordering (`feed`, `mine`, `user/{id}` vs `{id}`) resolves correctly without explicit constraints — a literal segment always wins over a route parameter in ASP.NET Core's routing.

`GetOwnPosts` (`/mine`) and `GetPostsByUser` (`/user/{userId}`) share one service method, `PostService.GetByUserAsync(userId, page, pageSize)` — they differ only in whether `userId` comes from `GetUserId()` or the route.

**Ownership checks** (`Update`/`Delete`) are done via a scoped query in the service — `WHERE Id = @id AND UserId = @userId` — rather than a separate resource-based authorization step. A dedicated authorization check (`IAuthorizationService.AuthorizeAsync` against a pre-fetched entity) would cleanly separate "is this allowed" from "do the update," but costs an extra DB round-trip to fetch the entity before the service call that touches it again; not worth it for a project this size. Both failure paths return `NotFound`, not `Forbidden`/`Unauthorized`, so a caller can't distinguish "post doesn't exist" from "post exists but isn't yours" — same oracle-avoidance reasoning as the auth failure messages.

**Visibility model, and the inconsistency it creates:** `feed`/`mine` are friends-gated (via `GetFeedAsync`'s friendship lookup); `GetById`/`GetAll`/`GetPostsByUser` are fully public. This isn't a bug — it mirrors real platforms where the home feed is curated but profile pages are public — but it means "who can see a post" currently depends on which endpoint is hit, not a single rule on the `Post` resource. Revisit if/when private accounts become a feature; that check would live in `GetPostsByUser`.

### `CommentController`

Route: `/comments`. Inherits `BaseController`. Same `[Authorize]`-at-controller-level-plus-`[AllowAnonymous]`-on-reads pattern as `PostsController`.

| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/comments/{id}` | Anonymous | Single comment |
| GET | `/comments/post/{postId}` | Anonymous | Paginated, comments for a post |
| GET | `/comments/mine` | Required | Caller's own comments, `userId` from JWT |
| POST | `/comments` | Required | Create |
| PUT | `/comments/{id}` | Required | Update, ownership-scoped |
| DELETE | `/comments/{id}` | Required | Delete, ownership-scoped |

Route ordering (`mine`, `post/{postId}` vs `{id}`) is safe for the same reason as `PostsController`.

**Ownership checks and NotFound-for-both** follow the exact same pattern as `PostsController`: scoped query (`WHERE Id = @id AND UserId = @userId`), `Update`/`Delete` return `NotFound` for both "doesn't exist" and "not yours."

No `/comments` (all-comments, unscoped) endpoint — unlike `PostsController`'s `GetAll`, there's no use case for listing every comment across the whole app; comments are always fetched scoped to a post or a user.

Backed by `CommentService`.

### Comment Display DTOs

Location: `/Common/Dtos/Comments/`.

Three tiers, mapped from `Comment` per-endpoint rather than via inheritance (each tier's shape genuinely diverges — not a strict superset of the one below — so manual typing keeps each response contract explicit and avoids polymorphic-serialization risk):

**`MinimalCommentDisplayDto`** — large/unscoped lists.

```csharp
public class MinimalCommentDisplayDto
{
    public int Id { get; set; }
    public string CommentContent { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
}
```

**`StandardCommentDisplayDto`** — paginated post-comments view (`GET /comments/post/{postId}`). Flat props, deliberately not nested — this tier is rendered in bulk, so the marginally cheaper flat shape is preferred over `UserSummaryDto`.

```csharp
public class StandardCommentDisplayDto
{
    public int Id { get; set; }
    public string CommentContent { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

**`FullCommentDisplayDto`** — single fetch (`GET /comments/{id}`).

```csharp
public class FullCommentDisplayDto
{
    public int Id { get; set; }
    public string CommentContent { get; set; } = string.Empty;
    public UserSummaryDto Author { get; set; } = null!;
    public int PostId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

`UserSummaryDto` (new, shared — not comment-specific):

```csharp
public class UserSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}
```

Introduced to replace the manual `userId`/`username` prop pattern used in `Post`'s DTOs — a single reusable shape for "who did this" that can grow (avatar, display name) without touching every DTO that references a user. `Post`'s existing DTOs were not retrofitted to use it.