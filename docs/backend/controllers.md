# WebCity — Controllers

> Part of the WebCity doc set. See also: `README.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

---

## Controllers Layer

All routes are prefixed `api/` (e.g. `api/posts`, not `/posts`). Nearly every action carries `[EnableRateLimiting(RateLimitConfig.Policies.X)]` — omitted from the tables below for brevity; see `auth.md`/`RateLimitConfig.cs` for the concrete limits (per-IP before login, per-user after; see the rate-limiting section of `auth.md`).

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
    [Range(Constants.FirstPageNumber, Constants.MaxPageNumber)]
    public int PageNumber { get; set; } = Constants.FirstPageNumber;

    [Range(Constants.MinPageSize, Constants.MaxPageSize)]
    public int PageSize { get; set; } = Constants.DefaultPageSize;
}

public class SearchUserQuery : PageQuery
{
    [MinLength(1), MaxLength(Constants.UsernameMaxlength)]
    public string Username { get; set; } = string.Empty;
}
```

Bound via `[FromQuery]`. Unlike the earlier nullable design, both properties now default and validate through `[Range]` — no more unpaginated "return everything" mode reachable from the API surface (internal/background callers still pass `null`/`null` directly to repository methods). `MaxPageNumber` keeps the offset `(PageNumber - 1) * PageSize` inside an `int`, so a page number below 1 or above it gets `400`, not a 500 from a negative or overflowing SQL offset.

---

### `PostController`

Route: `api/posts`. Inherits `BaseController`. `[Authorize]` at controller level, `[AllowAnonymous]` on public-read endpoints.

| Method | Route                     | Auth      | Notes                                              |
|--------|---------------------------|-----------|----------------------------------------------------|
| POST   | `api/posts`               | Required  | Create, `multipart/form-data`, with up to 10 photos |
| GET    | `api/posts/feed`          | Required  | Ranked posts by everyone but the caller            |
| GET    | `api/posts/mine`          | Required  | Caller's own posts                                 |
| GET    | `api/posts/{id}`          | Anonymous | Single post (with comments, split query)           |
| GET    | `api/posts/user/{userId}` | Anonymous | Any user's posts — public-profile-style visibility |
| PUT    | `api/posts/{id}`          | Required  | Update, ownership-scoped                           |
| DELETE | `api/posts/{id}`          | Required  | Delete, ownership-scoped **or admin**              |

`GetOwnPosts` (`/mine`) and `GetPostsByUser` (`/user/{userId}`) both call `PostService.GetByUserIdAsync`, which returns the posts newest first (ties by post id, newest first), so paging is deterministic.

**The ranked feed.** `GET api/posts/feed` (`PostService.GetFeedAsync` → `PostRepository.GetFeedAsync`) returns posts by friends and by other users, highest score first. Candidates are every post the caller did not write whose author is neither deactivated nor soft-deleted, so a user with no friends still gets a feed. The score is

```
score = (connection weight + likes + comments) / (age in hours + AgeOffsetHours) ^ AgeGravity
```

- **Connection weight**, checked in this order: `FriendWeight` when the author is an accepted friend of the caller, `MutualFriendWeight` when the author has an accepted friend who is also the caller's friend, `OtherUserWeight` for anyone else. A weight counts as that many likes or comments. Pending and declined requests count as no connection. A shared friend counts even if their account is deactivated or soft-deleted, as in `FriendshipRepository.GetFriendshipsAsync`.
- **Activity:** the post's like count plus its comment count, all comments included.
- **Age:** minute boundaries crossed from `CreatedAt` to the request time (`DATEDIFF(MINUTE, …)`, so 10:00:59 to 10:01:00 counts as one minute), divided by 60. A post dated in the future counts as zero hours old. `AgeOffsetHours` keeps a new post from dividing by zero, and a higher `AgeGravity` makes posts sink faster. Every weight is above zero, so even a post with no activity sinks as it ages.
- **Order:** highest score first, ties broken by newest post id, so equal scores keep their order on every request.
- **Two queries.** One raw SQL query (`PostRepository.RankedFeedPageSql`) scores every candidate, orders, pages, and returns only the page's post ids. Its CTEs each name one input: `CallerFriends`, `FriendsOfCallerFriends`, then `CandidatePosts` with `ConnectionWeight`, `Activity` and `AgeHours`. The SQL is raw because the LINQ version was unreadable, and it is not composable (SQL Server cannot wrap a CTE in a subquery), so it pages itself. A second query loads those posts with their images and stats, and the ids' order is restored in memory. All posts are never loaded. Raw SQL skips the global `User` filter, so the query checks the author's account state by hand.
- **Paging** is by page number, like every list. Scores change as time passes and posts gain likes, so a post can repeat or be skipped between two pages loaded minutes apart; that is accepted. Cursor paging and performance at scale are out of scope.
- The weights live only in `Common/ProjectConstants/FeedRanking.cs`, whose summary repeats this formula and says what the values mean in practice. They are a first guess, to be tuned once there is real traffic.

**Creating a post with photos.** `POST api/posts` takes `multipart/form-data` (`CreatePostDto`): `PostTitle` (required), `PostContent` (optional) and `Images` (zero or more files, in display order). A post needs a title and either text or at least one photo (`ResponseMessages.TextOrImageRequired`). A JSON body gets `415`. The response is `201` with the standard post shape. Every post read (`{id}`, `feed`, `mine`, `user/{userId}`) carries `Images`: a list of `ImageDto` (`Id`, `Address`, `OrderIndex`) in upload order. Editing stays text-only (`PUT` keeps its JSON body).

**Like and comment counts.** Every post read (`{id}`, `feed`, `mine`, `user/{userId}`) also carries `LikeCount`, `CommentCount` and `LikedByCaller`. `{id}` and `user/{userId}` stay anonymous: a signed-in caller gets their own `LikedByCaller` (`BaseController.GetOptionalUserId()`), and a guest always gets `false`. `PostRepository` counts in SQL through the `WithStats` projection (`PostWithStatsProjection`), as subqueries of the post query, so a page of posts costs one query, not one per post. A post that was just created returns zeros and `false` (`PostMapper.ToNewPostDisplay`).

**Images.** `ImageStorage` (`Infrastructure/Services/Storage`) owns all file handling, and nothing else touches the disk:

- **Checks, all before anything is saved.** At most `MaxImagesPerPost` files on a post (`TooManyPostImages`) or `MaxImagesPerMessage` on a message (`TooManyMessageImages`), each at most `MaxFileSizeBytes` (`ImageTooLarge`), and each a JPEG, PNG or WebP judged by its leading bytes, never by its name or declared content type (`ImageFormatNotAccepted`). If any file fails, the request is refused with `400` and no post, message or file is stored. Files and their owner are saved through `ImageStorage.SaveWithOwnerAsync`: it writes the files, hands their addresses to the callback that adds the post or message, and if a file write or that callback throws, deletes the files already written before the exception reaches `GlobalExceptionHandler`. `ImageMapper` builds the image rows and `ImageDto` lists for both owners.
- **Names and addresses.** Files are saved as `{guid}{extension}`, with the extension taken from the detected format; the client's file name is never used. The stored and returned address is relative: `/api/images/{name}`, so only `/api` and `/hubs` cross the proxy (ADR 0001).
- **Serving.** `UseImageStorage` (`Extensions/ImageStorageExtensions.cs`) serves the upload folder as static files at `/api/images`, before routing, CORS and the rate limiter. Anyone can fetch an image, signed in or not, because an image tag cannot send the in-memory access token (ADR 0004). Responses carry `X-Content-Type-Options: nosniff`.
- **Configuration** (`ImageStorageConfiguration`, section `ImageStorage`, every key optional): `UploadFolder` (default `Uploads`, relative to the content root, gitignored), `MaxFileSizeBytes` (default 5 MB), `MaxImagesPerPost` (default 10), `MaxImagesPerMessage` (default 5).
- **Request size.** The server's default request limit (30 MB) is smaller than ten 5 MB photos, so `PostUploadRequestSizeFilter` raises the body and multipart limits of `POST api/posts` to `MaxFileSizeBytes × MaxImagesPerPost` plus 1 MB for the text fields (`Constants.MultipartTextAllowanceBytes`). `MessageUploadRequestSizeFilter` sets the limits of `POST api/messages/{receiverId}` the same way from `MaxImagesPerMessage` (with the defaults that is 26 MB, a little below the server default, and still fits five photos at the size limit). Both derive from `ImageUploadRequestSizeFilter`.
- **Not yet built:** removing a photo, deleting a post's files with the post (a deleted post's `PostImages` rows cascade; its files stay on disk until the next orphan sweep).
- **File removal** is best effort and runs after the database change; a file that cannot be removed is logged by the storage. Message files go when a message is deleted, when a friendship is removed (which deletes the conversation) and when the purge of soft-deleted users deletes messages (`background-jobs.md`).
- **Files left behind** (a crash between saving the files and their owner, a failed best-effort delete, a purge that stopped partway, a deleted post) are removed by the backstop job `OrphanImagesCleanupService`. It deletes files in the upload folder older than `MinimumAge` (default 1 hour) that no `PostImages` or `MessageImages` row refers to (`background-jobs.md`).

**Ownership checks** (`Update`) remain a scoped comparison in the service (`post.UserId == userId`) rather than a separate authorization step. **Delete now also accepts admins**: `DeletePostAsync(callerId, isAdmin, id)` allows the post owner *or* a caller with the `Administrator` role, sourced from `BaseController.IsAdministrator()`.

**Visibility model.** Every post is readable by everyone: `GetById` and `GetPostsByUser` are public, `feed` shows any other user's post to a signed-in caller, and `mine` returns the caller's own. Friendship changes only a post's rank in the feed, never whether it can be seen.

Deleting a post runs inside a transaction: comments are deleted first (`CommentRepository.DeletePostCommentsAsync`), then the post (`DeleteWithoutChangeTrackingAsync`), avoiding change-tracker/FK ordering issues. The post's likes go with it through the database cascade.

### `PostLikeController`

Route: `api/posts/{postId}/likes`. Inherits `BaseController`. `[Authorize]` at controller level, `[AllowAnonymous]` on the read. Rate-limit policies: `LikePost`, `UnlikePost`, `ReadPostLikes`, all per user.

| Method | Route                      | Auth      | Notes                                         |
|--------|----------------------------|-----------|-----------------------------------------------|
| POST   | `api/posts/{postId}/likes` | Required  | Like as the caller. `200`, or `404`           |
| DELETE | `api/posts/{postId}/likes` | Required  | Remove the caller's like. `200`, or `404`     |
| GET    | `api/posts/{postId}/likes` | Anonymous | Paginated likers, newest first. `200`, or `404` |

- **Like** (`PostLikeService.LikePostAsync`): a user likes a post at most once. A repeated like also gets `200` and `PostLiked`, but adds no row and sends no push, so the client never sees a failure for a like that is already there. An unknown post gets `404` (`PostNotFound`). Any post can be liked, the caller's own included, and so can the post of a deactivated or deleted account, because such posts are still readable. On success the post's author gets a `PostLiked` push, unless they liked their own post (see `signalr.md`).
- **Unlike**: an unknown post (`PostNotFound`) or a post the caller has not liked (`PostNotLiked`) gets `404`. A user can like the post again after unliking it.
- **Likers**: a `ListResponse<MinimalUserDto>`, newest like first (ties by like id, newest first), paged by `PageQuery`. A liker whose account is deactivated or deleted shows the placeholder username from `UsernameMapper`. An unknown post gets `404`; a post with no likes gets `200` and an empty list.
- **Logging.** Only failures (an unknown post, an unlike of a post not liked) write a `Log` row. A successful like or unlike does not, because both happen often and the `PostLike` row is the record.
- A simultaneous double like from one user hits the unique index on (`PostId`, `UserId`), and the losing request gets `409` with the generic envelope from `GlobalExceptionHandler`, though a repeated like is otherwise a success; the data stays correct.

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

**Delete permission is three-way**: `belongsToCaller || isPostAuthor || isAdmin` — a post author can remove any comment on their own post, not just the comment's author.

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

Removing a relationship also deletes the pair's message history (`MessageRepository.DeleteConversationAsync`) before deleting the `Friendship` row. The conversation's photo addresses are read first, and the files are deleted after both rows are gone.

### `MessageController`

Route: `api/messages`. Inherits `BaseController`.

No `GetMessage(id)` action — a single message isn't fetched by id from the client; `SendMessage`'s `Created()` response builds its location string manually rather than via `CreatedAtAction`.

| Method | Route                                                  | Notes                                                                                                                                                    |
|--------|--------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| POST   | `api/messages/{receiverId}`                            | Send a message. Body: `CreateMessageDto` as `multipart/form-data`. Response: `SentMessageDto`. Pushes `ReceiveMessage` (payload `PushMessageDto`) to the receiver's SignalR group |
| GET    | `api/messages/conversation/{otherUserId}`              | Paginated (`PageQuery`), `ListResponse<StandardMessageDto>`. Also marks the caller's unread incoming messages in that conversation as read               |
| GET    | `api/messages/friends/conversation`                    | Caller's friends with an existing conversation (chat list); `ListResponse<ConversationFriendDto>`                                                        |
| GET    | `api/messages/friends/no-conversation`                 | Caller's friends with no conversation yet; `ListResponse<DisplayFriendDto>`                                                                              |
| GET    | `api/messages/conversations/unread`                    | `{ UnreadCount: int }` — distinct-sender count of unread messages, via `GetUnreadConversationsCount`                                                     |
| PUT    | `api/messages/conversation/{otherUserId}/mark-as-read` | Mark the conversation as read. No content                                                                                                                |
| PUT    | `api/messages/{id}`                                    | Edit own message, sender-scoped, allowed while the message is younger than `Constants.EditMessageWindow` (20 minutes). Pushes `MessageEdited` to the receiver |
| DELETE | `api/messages/{id}`                                    | Delete own message, sender-scoped. Not time-limited. Removes the message's photo rows (cascade) and then its files                                       |

**`ValidFriendship` check** (`MessageService`) gates `SendMessage`: rejects self-messaging, nonexistent receiver, and non-friends, collapsing to one `ApplicationResponse.Fail` → `BadRequest`, same oracle-avoidance reasoning as elsewhere.

**Sending with photos.** `POST api/messages/{receiverId}` takes `multipart/form-data` (`CreateMessageDto`): `Content` (optional text) and `Images` (zero to `MaxImagesPerMessage` files, default 5, in display order). A JSON body gets `415`. `MessageService.CheckNewMessageAsync` runs, in order, the friendship check, the text-or-photo check (`ResponseMessages.MessageTextOrImageRequired`; whitespace-only text counts as no text, though with photos it is stored as sent, as for posts) and `ImageStorage.CheckMessageImagesAsync`. Any failure is `400` with nothing saved, and one bad file refuses the whole message. The same storage, checks, naming and serving apply as for post photos (see **Images** under `PostController`); message photos are protected by their unguessable name only. `SentMessageDto`, the `ReceiveMessage` push (`PushMessageDto`) and the conversation history (`StandardMessageDto`) each carry `Images`, a list of `ImageDto` in upload order. Editing stays a JSON, text-only request; the photos of a sent message cannot be changed.

**Repository note:** `GetConversationFriendsAsync` uses raw SQL (`SqlQueryRaw<T>`) — grouped latest-message-per-partner via `CROSS APPLY`, plus explicit `IsDeleted`/`IsDeactivated` bit-cast columns since raw SQL bypasses the `User` query filter entirely. A subquery counts the last message's `MessageImages` rows into `LastMessageImageCount`, so the chat list can show something for a photo with no text. Projection type (`ConversationFriendProjection`) is flat; reshaped into `ConversationFriendDto` in the mapper.

### `AuthController`

Route: `api/auth`. `[ApiController]` directly (not `BaseController` — no authenticated user context needed for any action here). See `auth.md` for the full flow (register/login/refresh/logout/password-reset) and for the two failure shapes any controller can return.

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
| PATCH  | `api/accounts/{id}/deactivate`  | `Administrator` | Deactivate another account — `Administrator` role only (`[Authorize(Roles = nameof(RoleName.Administrator))]`)                |
| PATCH  | `api/accounts/{id}/activate`    | `Administrator` | Admin-reactivate, immediate                                                                                                   |
| PATCH  | `api/accounts/activate`         | Anonymous       | Self-reactivate via emailed token (body: `ActivateAccountDto`)                                                                |
| PATCH  | `api/accounts/request-activate` | Anonymous       | Request a reactivation token by email (body: `ActivationRequestDto`)                                                          |

See `auth.md` for the full activation/deactivation flow and email notifications.

**Rate-limit policies.** `mine/deactivate` uses `Policies.DeactivateOwnAccount` (5/hour); the admin route `{id}/deactivate` uses `Policies.AdminDeactivateAccount` (30/hour).

**Who may deactivate whom.** Self-deactivation needs only a signed-in caller. Deactivating another user's account is `Administrator`-only; any other signed-in caller gets `403`.

### `AdminController`

Route: `api/admin`. Inherits `BaseController`. The whole controller is `[Authorize(Roles = nameof(RoleName.Administrator))]`: a guest gets `401`, any other signed-in caller `403`. Later tickets add statistics and moderation to it.

| Method | Route             | Notes                                                                                                                       |
|--------|-------------------|-----------------------------------------------------------------------------------------------------------------------------|
| GET    | `api/admin/logs`  | `Log` rows, newest first, `LogQueryDto`, `ListResponse<LogDisplayDto>`. Policy `AdminReadLogs` (120/minute)                  |
| GET    | `api/admin/users` | Accounts in any state, oldest account first, `AccountListQuery`, `ListResponse<AdminUserDto>`. Policy `AdminListAccounts` (120/minute) |

**Log filters** (`LogQueryDto : PageQuery`, all optional, combined with AND): `userId`, `authorizedRequest`, `succeeded`, `entityName` (exact), `entityId`, `action` (matches any part, so `action=PostService` finds every `PostService` method), and `from` / `to` (both inclusive, UTC). A `from` later than `to` is refused by `LogQueryDto.Validate` with `400` and the framework's validation `ProblemDetails` (`ResponseMessages.InvalidDateRange`), like any other query-shape error. Rows written in the same millisecond are ordered by id, newest first, so paging is deterministic. `LogService` reads through `LogRepository.SearchLogsAsync`, not the `DbContext`.

**Account list** (`AccountListQuery : PageQuery`): `state` (`Active`, `Deactivated` or `SoftDeleted`) and `username` (matches any part), both optional. It ignores the `User` query filter, which is how an administrator finds a deactivated account to restore with `PATCH api/accounts/{id}/activate`. Each `AdminUserDto` row carries id, username, email, `isAdministrator`, `state` (written as text), `registeredAt`, `deactivatedAt` and `deletedAt`. A soft-deleted account reads as `SoftDeleted` even if it was also deactivated. This endpoint is not in the documentation's endpoint list; it was added because the admin panel cannot restore an account without it.

**No log row on success.** Neither read writes a `Log` row when it succeeds, so reading the logs does not fill them. (`UserService.SearchUsersAsync`, by contrast, logs every search.)
