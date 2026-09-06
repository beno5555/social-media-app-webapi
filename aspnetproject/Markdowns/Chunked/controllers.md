# AspNetProject — Controllers

> Part of the AspNetProject doc set. See also: `core.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

---

## Controllers Layer

All routes are prefixed `api/` (e.g. `api/posts`, not `/posts`). Nearly every action carries `[EnableRateLimiting(RateLimitConfig.Policies.X)]` — omitted from the tables below for brevity; see `auth.md`/`RateLimitConfig.cs` for the concrete limits.

### `BaseController`

```csharp
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected int    GetUserId()       // NameIdentifier claim, parsed to int
    protected string GetUsername()     // Name claim
    protected bool   IsAdministrator() // User.IsInRole(nameof(RoleName.Administrator))
}
```

`[Authorize]` is applied per-controller, not here, so controllers with mostly-public endpoints can invert it with `[AllowAnonymous]`.

### `PageQuery` / `SearchUserQuery`

In `/Infrastructure/Queries/`:

```csharp
public class PageQuery
{
    public int PageNumber { get; set; } = 1;

    [Range(1, Constants.MaxPageSize)]
    public int PageSize { get; set; } = Constants.DefaultPageSize;
}

public class SearchUserQuery : PageQuery
{
    [MinLength(1), MaxLength(Constants.UsernameMaxlength)]
    public string Username { get; set; } = string.Empty;
}
```

Bound via `[FromQuery]`. Unlike the earlier nullable design, both properties now default and validate (`PageSize` capped via `[Range]`) — no more unpaginated "return everything" mode reachable from the API surface (internal/background callers still pass `null`/`null` directly to repository methods).

---

### `PostController`

Route: `api/posts`. Inherits `BaseController`. `[Authorize]` at controller level, `[AllowAnonymous]` on public-read endpoints.

| Method | Route                     | Auth      | Notes                                              |
|--------|---------------------------|-----------|----------------------------------------------------|
| POST   | `api/posts`               | Required  | Create                                             |
| GET    | `api/posts/feed`          | Required  | Friends-only, via `PostService.GetFeedAsync`       |
| GET    | `api/posts/mine`          | Required  | Caller's own posts                                 |
| GET    | `api/posts/{id}`          | Anonymous | Single post (with comments, split query)           |
| GET    | `api/posts/user/{userId}` | Anonymous | Any user's posts — public-profile-style visibility |
| PUT    | `api/posts/{id}`          | Required  | Update, ownership-scoped                           |
| DELETE | `api/posts/{id}`          | Required  | Delete, ownership-scoped **or admin**              |

`GetOwnPosts` (`/mine`) and `GetPostsByUser` (`/user/{userId}`) both call `PostService.GetByUserIdAsync`.

**Ownership checks** (`Update`) remain a scoped comparison in the service (`post.UserId == userId`) rather than a separate authorization step. **Delete now also accepts admins**: `DeletePostAsync(callerId, isAdmin, id)` allows the post owner *or* a caller with the `Administrator` role, sourced from `BaseController.IsAdministrator()`.

**Visibility model, and the inconsistency it creates:** `feed`/`mine` are friends-gated; `GetById`/`GetPostsByUser` are fully public. Mirrors real platforms where the home feed is curated but profile pages are public — but "who can see a post" still depends on which endpoint is hit, not a single rule on the `Post` resource.

Deleting a post runs inside a transaction: comments are deleted first (`CommentRepository.DeletePostCommentsAsync`), then the post (`DeleteWithoutChangeTrackingAsync`), avoiding change-tracker/FK ordering issues.

### `CommentController`

Route: `api/comments`. Inherits `BaseController`. Same `[Authorize]`-at-controller-level-plus-`[AllowAnonymous]`-on-reads pattern.

| Method | Route                         | Auth      | Notes                                     |
|--------|-------------------------------|-----------|-------------------------------------------|
| POST   | `api/comments/posts/{postId}` | Required  | Create                                    |
| GET    | `api/comments/mine`           | Required  | Caller's own comments                     |
| GET    | `api/comments/post/{postId}`  | Anonymous | Paginated, comments for a post            |
| GET    | `api/comments/{id}`           | Anonymous | Single comment                            |
| PUT    | `api/comments/{id}`           | Required  | Update, ownership-scoped                  |
| DELETE | `api/comments/{id}`           | Required  | Delete — commenter, post author, or admin |

Note the create/read route naming is inconsistent: creating posts a comment under `api/comments/posts/{postId}` (plural `posts`), while listing a post's comments reads from `api/comments/post/{postId}` (singular `post`) — not a typo to "fix" without checking client code depends on it.

**Delete permission is three-way**: `belongsToCaller || isPostAuthor || isAdmin` — a post author can remove any comment on their own post, not just the comment's author. Wider than the plain ownership check the console-migration doc originally described.

**Live notification**: creating a comment pushes a `ReceiveComment` SignalR event (payload: `NotifyCommentDto` — Id, AuthorUsername, Content) to the post author's group, unless the commenter is the post author themselves. See `signalr.md`.

No `api/comments` (all-comments, unscoped) endpoint — comments are always fetched scoped to a post or a user.

### `FriendshipController`

Route: `api/friendships`. Inherits `BaseController`. `[Authorize]` at controller level, no `[AllowAnonymous]` overrides — no public-read case for friendship data.

| Method | Route                                   | Notes                                                                                                                                                          |
|--------|-----------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| POST   | `api/friendships/{addresseeId}`         | Send a friend request                                                                                                                                          |
| GET    | `api/friendships/{friendId}`            | Caller's relationship with a user, **any status**                                                                                                              |
| GET    | `api/friendships/accepted/{friendId}`   | Caller's relationship with a user, **accepted-only**, richer DTO                                                                                               |
| GET    | `api/friendships/accepted`              | Caller's own accepted friends (`PageQuery`)                                                                                                                    |
| GET    | `api/friendships/pending`               | Incoming requests (caller is addressee)                                                                                                                        |
| GET    | `api/friendships/sent`                  | Outgoing requests (caller is requester)                                                                                                                        |
| GET    | `api/friendships/user/{userId}`         | Any user's accepted friends — reuses `GetFriendshipsAsync` against the route's `userId`, not the caller; shows *that user's* friend list, public-profile-style |
| PUT    | `api/friendships/{requesterId}/accept`  | Accept a pending request                                                                                                                                       |
| PUT    | `api/friendships/{requesterId}/decline` | Decline a pending request                                                                                                                                      |
| DELETE | `api/friendships/{friendId}`            | Remove an existing relationship                                                                                                                                |

**Two tiers of "get relationship."** `GetRelationship` (`GET /{friendId}`) returns any status via `StandardFriendshipDto` (other user as `MinimalUserDto`, `Status`, `SentAt`, `LastUpdatedAt`). `GetAcceptedFriendship` (`GET /accepted/{friendId}`) is accepted-only and returns `AcceptedFriendshipDto` (other user as the richer `DisplayFriendDto`, which includes `LastActiveAt`/`IsActive` presence data) — a deliberately different shape for the "you're already friends" case versus the general relationship-status case.

**`Friendship` now has an `Id`** (inherits `BaseEntity`), but routes still identify relationships by *the other user's id*, not `Friendship.Id` — unchanged in spirit from the original composite-PK design even though the underlying PK isn't composite anymore.

**Declined-request resend logic** (`FriendshipService.HandleExistingRelationship`): if the *original addressee* now sends a request back to the original requester, the old `Declined` row is deleted and a fresh `Friendship` created with the roles reversed; if the *original requester* re-sends, the same row is flipped back to `Pending` with `SentAt` refreshed in place (no new row).

**Status-code mapping.** `ApplicationResponse` carries only a bool + message, no error-type enum, so the controller can only safely distinguish HTTP statuses when a service method's failure paths share one meaning:
- `RemoveRelationship` — single failure path ("not found") → `NotFound`.
- `SendRequest` — several distinct failure meanings (addressee not found / self-request / already friends / pending exists) collapse to `BadRequest`.
- `RespondToRequest` — collapses to `BadRequest` similarly.

Removing a relationship also deletes the pair's message history (`MessageRepository.DeleteConversationAsync`) before deleting the `Friendship` row.

### `MessageController`

Route: `api/messages`. Inherits `BaseController`.

No `GetMessage(id)` action — a single message isn't fetched by id from the client; `SendMessage`'s `Created()` response builds its location string manually rather than via `CreatedAtAction`.

| Method | Route                                                  | Notes                                                                                                                                                    |
|--------|--------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| POST   | `api/messages/{receiverId}`                            | Send a message. Body: `CreateMessageDto`. Response: `SentMessageDto`. Pushes `ReceiveMessage` (payload `PushMessageDto`) to the receiver's SignalR group |
| GET    | `api/messages/conversation/{otherUserId}`              | Paginated (`PageQuery`), `ListResponse<StandardMessageDto>`. Also marks the caller's unread incoming messages in that conversation as read               |
| GET    | `api/messages/friends/conversation`                    | Caller's friends with an existing conversation (chat list); `ListResponse<ConversationFriendDto>`                                                        |
| GET    | `api/messages/friends/no-conversation`                 | Caller's friends with no conversation yet; `ListResponse<DisplayFriendDto>`                                                                              |
| GET    | `api/messages/conversations/unread`                    | `{ UnreadCount: int }` — distinct-sender count of unread messages, via `GetUnreadConversationsCount`                                                     |
| PUT    | `api/messages/conversation/{otherUserId}/mark-as-read` | Mark the conversation as read. No content                                                                                                                |
| PUT    | `api/messages/{id}`                                    | Edit own message, sender-scoped, time-limited to `Constants.EditMessageWindow` (20 minutes). Pushes `MessageEdited`                                      |
| DELETE | `api/messages/{id}`                                    | Delete own message, sender-scoped. Not time-limited                                                                                                      |

**`ValidFriendship` check** (`MessageService`) gates `SendMessage`: rejects self-messaging, nonexistent receiver, and non-friends, collapsing to one `ApplicationResponse.Fail` → `BadRequest`, same oracle-avoidance reasoning as elsewhere.

**Repository note:** `GetConversationFriendsAsync` uses raw SQL (`SqlQueryRaw<T>`) — grouped latest-message-per-partner via `CROSS APPLY`, plus explicit `IsDeleted`/`IsDeactivated` bit-cast columns since raw SQL bypasses the `User` query filter entirely. Projection type (`ConversationFriendProjection`) is flat; reshaped into `ConversationFriendDto` in the mapper.

**Possible bug — edit-window comparison looks inverted.** `EditMessage`'s check is `bool canEdit = DateTime.UtcNow - message.CreatedAt > Constants.EditMessageWindow;` — as written, `canEdit` is `true` once *more* time than the window has elapsed (i.e. after the window closes), and `false` while still inside it. That's the opposite of "editable within a 20-minute window" as described elsewhere in this doc set and in `Constants.EditMessageWindow`'s name. Worth confirming against actual behavior before relying on either reading.

### `AuthController`

Route: `api/auth`. `[ApiController]` directly (not `BaseController` — no authenticated user context needed for any action here). See `auth.md` for the full flow (register/login/refresh/logout/password-reset).

### `UserController`

Route: `api/users`. Inherits `BaseController`. `[Authorize]` at controller level, `[AllowAnonymous]` on public reads.

| Method | Route                  | Auth            | Notes                                                                                            |
|--------|------------------------|-----------------|--------------------------------------------------------------------------------------------------|
| POST   | `api/users`            | `Administrator` | Admin-create account, delegates to `AuthService.RegisterAsync`                                   |
| GET    | `api/users/{id:int}`   | Anonymous       | Single user by id, `StandardUserDto`                                                             |
| GET    | `api/users/{username}` | Anonymous       | Single user by username, `StandardUserDto` (route-constraint-disambiguated from the id overload) |
| GET    | `api/users/mine`       | Required        | Caller's own full profile, `FullUserDto` (includes email + roles)                                |
| GET    | `api/users/search`     | Anonymous       | Username-substring search, `SearchUserQuery`, `ListResponse<MinimalUserDto>`                     |
| PUT    | `api/users/mine`       | Required        | Edit own profile (username, DOB, bio)                                                            |
| PUT    | `api/users/{id:int}`   | `Administrator` | Edit another user's profile                                                                      |

**`POST api/users` reuses `AuthService.RegisterAsync`** rather than a separate admin-create path in `AccountManagementService` — unlike the earlier design note that anticipated keeping admin-create and self-registration deliberately separate, they now share one code path; admin-vs-self distinction is only the route/authorization, not the underlying service call.

**DTO tiers**, shape-by-audience:
- `FullUserDto` — Id, Username, Email, Bio, `Roles` (list of role names), RegisteredAt, DateOfBirth. Only returned for the caller's own profile or a freshly-registered account.
- `StandardUserDto` — Id, Username, Bio, RegisteredAt, DateOfBirth (no email, no roles). Public single-fetch.
- `MinimalUserDto` — nullable Id + Username. List/search results.

**Username change cooldown.** `User.UsernameLastChangedAt` (nullable, null = never changed). `EditUserProfileAsync` only blocks the edit if the username itself changed and the cooldown (`Constants.UsernameChangeCooldownDays`, 20 days) hasn't elapsed; a bio/DOB-only edit never touches this field or the cooldown. Collapses into the same mixed-meaning `BadRequest` bucket ("not found" vs. "cooldown active" aren't told apart at the HTTP layer).

**DOB validation** via `ValidAgeAttribute` (`Constants.MinAge`/`MaxAge` = 13/100, rejects future dates), mirroring the DB check constraint; shared between `RegisterDto` and `EditUserDto`.

### `AccountManagementController`

Route: `api/accounts`. Inherits `BaseController`. `[Authorize]` at controller level.

| Method | Route                                    | Auth            | Notes                         |
|--------|------------------------------------------|-----------------|-------------------------------|
| PUT    | `api/accounts/{id}/assign-administrator` | `Administrator` | Grant the Administrator role  |
| PUT    | `api/accounts/{id}/remove-administrator` | `Administrator` | Revoke the Administrator role |
| DELETE | `api/accounts/mine`                      | Required        | Soft-delete own account       |
| DELETE | `api/accounts/{id}`                      | `Administrator` | Soft-delete another account   |

**Soft delete, not hard delete.** `SoftDeleteAccountAsync` runs inside `ExecuteInTransactionAsync`: clears the tracker, revokes all refresh tokens, then sets `AccountDeletedAt` (idempotency-checked — fails if already soft-deleted). No cascading data removal happens here; that's the background job's job (see `background-jobs.md`).

**Role assignment** goes through `UserRoleRepository`/`RoleRepository` directly — checks the user exists, the `Administrator` role row exists (seeded, so effectively always), and the `UserRole` pairing doesn't already exist before inserting/deleting.

### `AccountActivationController`

Route: `api/accounts` (shares the prefix with `AccountManagementController` — they're two controllers on the same route, split by concern rather than by URL segment). Inherits `BaseController`.

| Method | Route                           | Auth            | Notes                                                                                                                         |
|--------|---------------------------------|-----------------|-------------------------------------------------------------------------------------------------------------------------------|
| PATCH  | `api/accounts/mine/deactivate`  | Required        | Self-deactivate                                                                                                               |
| PATCH  | `api/accounts/{id}/deactivate`  | Required        | Deactivate another account — no `[Authorize(Roles = ...)]`, reachable by any authenticated caller despite the "admin" framing |
| PATCH  | `api/accounts/{id}/activate`    | `Administrator` | Admin-reactivate, immediate                                                                                                   |
| PATCH  | `api/accounts/activate`         | Anonymous       | Self-reactivate via emailed token (body: `ActivateAccountDto`)                                                                |
| PATCH  | `api/accounts/request-activate` | Anonymous       | Request a reactivation token by email (body: `ActivationRequestDto`)                                                          |

See `auth.md` for the full activation/deactivation flow and email notifications.

**Rate-limit policy names look swapped.** `mine/deactivate` (self-service) is annotated `[EnableRateLimiting(RateLimitConfig.Policies.AdminDeactivateAccount)]` (30/hour — generous), while `{id}/deactivate` (deactivating *another* account, unrestricted by role as noted above) is annotated `Policies.DeactivateOwnAccount` (5/hour — restrictive). The policy names read backwards relative to which route they're on; worth confirming which limit was actually intended for which route before treating either as load-bearing for abuse prevention.
