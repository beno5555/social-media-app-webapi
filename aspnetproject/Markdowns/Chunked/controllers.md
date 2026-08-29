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

### `FriendshipController`

Route: `/friendships`. Inherits `BaseController`. `[Authorize]` at the controller level, with **no** `[AllowAnonymous]` overrides — unlike `PostsController`/`CommentController`, friendship data has no public-read case; every action requires an authenticated caller.

| Method | Route | Notes |
|---|---|---|
| GET | `/friendships/{otherUserId}` | Caller's relationship with a specific user, order-independent |
| GET | `/friendships/mine` | Caller's own accepted friends |
| GET | `/friendships/pending-requests` | Incoming requests (caller is addressee) |
| GET | `/friendships/sent-requests` | Outgoing requests (caller is requester) |
| GET | `/friendships/user/{userId}` | Any user's accepted friends, public-profile-style view |
| POST | `/friendships/{addresseeId}` | Send a friend request |
| PUT | `/friendships/{requesterId}/accept` | Accept a pending request |
| PUT | `/friendships/{requesterId}/decline` | Decline a pending request |
| DELETE | `/friendships/{friendId}` | Remove an existing relationship |

**No `{id}` route, by design.** `Friendship` has a composite PK (`RequesterId`, `AddresseeId`) and doesn't inherit `BaseEntity`, so there's no single `Id` to route on the way `PostsController`/`CommentController` do. Every mutating route instead identifies the relationship by *the other user's id*, with the caller's own id always coming from `GetUserId()`. This also makes ownership implicit rather than a separate check: passing `GetUserId()` as `addresseeId` in `RespondToRequestAsync`, for example, means only the actual addressee can accept/decline a given `requesterId` — the service's own "is this pending, for this pair" lookup fails to match otherwise.

**`GetFriends` lives at `/mine`, not the bare route.** Matches the `-mine` convention already used on Posts/Comments. Unlike Posts, there's no legitimate "list every friendship in the system" use case for Friendships (privacy, not just scale), so the bare `/friendships` route is simply unused rather than repurposed for something else.

**Self-request guards live in the service, not the controller.** 

**Status-code mapping.** `ApplicationResponse` carries only a bool + message, no error-type enum, so the controller can only safely distinguish HTTP statuses when a service method's failure paths share one meaning:
- `RemoveRelationship` — single failure path ("not found") → `NotFound`.
- `SendRequest` — three different failure meanings share one `Fail(...)` call (addressee not found / already friends / pending exists already), indistinguishable without parsing `Message` text → all collapse to `BadRequest`.
- `RespondToRequest` — similarly collapses to `BadRequest` since self-request and no-pending-request failures aren't told apart.
  **No 2-user-id admin lookup.** An endpoint letting any caller check the relationship between two *arbitrary* other users was considered and rejected — without a role/permission system (deferred indefinitely for this project), it's a straightforward privacy leak. Revisit only if an admin panel with real roles gets built.

---

### `MessageController`

Route: `/messages`. Inherits `BaseController`. 

No `Update`/`Delete` actions — not supported by the service; deferred, not designed.

No `GetMessage(id)` action — no real chat UX fetches a single message by id (conversations are bulk-loaded per page; per-message "details" are fields already present in the row, not a new fetch). `SendMessage`'s `Created()` response uses a manually built location string instead of `CreatedAtAction`, so no phantom endpoint exists just to support it.

| Method | Route                                      | Notes                                                                                                                                                                                                                                                                                                                                                                            |
|--------|--------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| POST   | `/messages/{receiverId}`                   | Send a message. Receiver in the route, not the DTO — same convention as `FriendshipController.SendRequest`. Body: `CreateMessageDto` (`MessageContent` only). Response: `SentMessageDto`                                                                                                                                                                                         |
| GET    | `/messages/conversation/{otherUserId}`     | Paginated (`PageQuery`), returns `ListResponse<StandardMessageDto>`. Fetching also marks the caller's unread incoming messages as read (reference-type mutation off `MarkAsReadAsync`, no separate loop needed). `ValidFriendship` failures collapse to `BadRequest` — same as `FriendshipController.SendRequest`, can't distinguish failure meanings from `ApplicationResponse` |
| GET    | `/messages/conversations`                  | Caller's friends with an existing conversation (chat list). Returns `List<ConversationFriendDto>`                                                                                                                                                                                                                                                                                |
| GET    | `/messages/friends/no-conversation`        | Caller's friends with no conversation yet (start-new-chat picker). Returns `List<UserSummaryDto>` directly — no dedicated DTO, no relationship metadata exists yet for this pairing                                                                                                                                                                                              |
| GET    | `/messages/unread`                         | Unread conversation count (int), via `GetUnreadConversationsCount`                                                                                                                                                                                                                                                                                                               |
| PUT    | `/messages/conversation/{id}/mark-as-read` | Mark the entire conversation as read. No content.                                                                                                                                                                                                                                                                                                                                |
| PUT    | `/messages/{id}`                           | Edit own message, sender-scoped. Time-limited to a configurable edit window.                                                                                                                                                                                                                                                                                                     |
| DELETE | `/messages/{id}`                           | Delete own message, sender-scoped. Not time-limited                                                                                                                                                                                                                                                                                                                              |

**`GetConversationFriends`/`GetNonConversationFriends` live here despite returning user-shaped DTOs** — controller placement follows which service backs the logic, not the response DTO's shape. Same reasoning as `GetPostsByUser` living under `/posts`.

**Repository note:** conversation-friends query uses raw SQL (`SqlQueryRaw<T>`), not LINQ — grouped latest-message-per-partner plus a conditional join key was judged too high-risk for silent EF translation failure/client-eval fallback. Projection type is flat (`FriendId`, `FriendUsername`, ...) — `SqlQueryRaw<T>` can't map onto nested/owned types; nesting into `UserSummaryDto` happens in the mapper afterward.

### `AccountController`

Route: `/accounts`. Inherits `BaseController`. `[Authorize]` at controller level, with `[AllowAnonymous]` on public reads — same pattern as `PostsController`/`CommentController`.

| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/accounts/{username}` | Anonymous | Single user, public view. Returns `StandardUserDto` (no email) |
| GET | `/accounts/search` | Anonymous | Username substring search, paginated via `SearchUsersQuery : PageQuery`. Returns `UserSummaryDto` (id + username only) |
| PUT | `/accounts/mine` | Required | Edit own profile (username, DOB, bio — email excluded). Returns `UserDetailDto` |
| DELETE | `/accounts/mine` | Required | Delete own account. No content |
| POST | `/accounts` | Required, `Roles = "Admin"` | Admin-create. Returns `UserDetailDto` |

No bare `GET /accounts` (list-all). Considered and dropped — no concrete admin or user workflow needs an unfiltered, unranked dump of every user; search already covers lookup, and a real admin queue would need filters/sort this doesn't have.

**DTO tiers**, mirroring the Friendship pattern of shape-by-audience rather than one DTO for everything:
- `UserDetailDto` — full, includes email. Used only where the caller is looking at a record they have elevated claim to: their own profile after an edit, or a newly admin-created account.
- `StandardUserDto` — everything `UserDetailDto` has, minus email. Public single-fetch (`GetByUsername`).
- `UserSummaryDto` — id + username only. List/search results, on the theory that browsing a result set shouldn't pull full profile data the caller may never open; a second fetch (`GetByUsername`) covers the rest if needed.

**`POST /accounts` routes through `AccountService`, not `AuthService`**, despite functionally creating a user the same way registration does — kept separate so admin-created-account messaging/response shape can diverge from self-registration without `AuthService.RegisterAsync` growing a caller-context branch. `[Authorize(Roles = "Admin")]` is currently inert: no role claims are issued anywhere in the project (RBAC deferred indefinitely), so this fails closed rather than open — endpoint is unreachable by design until roles exist, not a gap to patch now.

**Username change cooldown.** `User.UsernameLastChangedAt` (nullable `DateTime`, null = never changed). `UpdateProfileAsync` (service-level) only rejects a username change if one already happened within the cooldown window; a bio/DOB-only edit never touches this field. Collapses into the same mixed-meaning `BadRequest` bucket as `FriendshipController`'s `SendRequest`/`RespondToRequest` — "not found" vs. "cooldown active" aren't told apart at the HTTP layer, same reasoning (`ApplicationResponse` has no error-type enum).

**DOB validation** via a custom `ValidAgeAttribute` (13–130, rejects future dates) on the DTO, mirroring the existing DB check constraint rather than duplicating magic numbers inline; shared between `RegisterDto` and the edit-profile DTO.