# AspNetProject

An ASP.NET Core Web API, migrated from an earlier C# console application that simulated a basic social media platform. Core domain logic (entities, EF Core configuration, repositories, mappers, DTOs) carried over from the console app largely unchanged; the console-specific presentation layer was removed and replaced with a proper API layer.

---

## Migration Context

This project began as a console application using EF Core code-first, layered architecture, and manual DI. The console app's job was learning-focused: build the domain and data layers correctly, with a hand-rolled menu system standing in for a UI.

Moving to a Web API meant replacing the presentation layer, not the domain. The menu system was doing the same job controllers do — routing user actions to services and rendering results — but shaped around a single-user, stateful console session. That shape doesn't fit a stateless, multi-client API.

### Carried over as-is

- Entities and `BaseEntity`
- `AppDbContext` and entity configurations
- Repositories (`BaseRepository<T>`, `BaseEntityRepository<T>`, and specific repositories) — concrete classes, no interfaces
- Mappers
- DTOs (base structure retained, expanded per endpoint as needed)
- `Constants.cs`

### Dropped

- `Printer`, `Prompter`, `DtoPrompter` — console I/O, no equivalent in an API
- All menus (`BaseMenu`, `MainMenu`, `UnauthenticatedMenu`, `AuthenticatedMenu`, `PostMenu`, `FriendMenu`, `MessageMenu`) — replaced by controllers
- `PaginatedInput`, `ConversationInput` — console input shapes; pagination now comes from query parameters
- `NavigateToRootException`, `AccountDeletedException` — solved stack-unwinding in a console menu loop; no equivalent in a stateless request/response cycle
- `SessionUser` in its original form — was a scoped, mutable object tied to a persistent login session; being reworked around reading claims from `HttpContext.User` per request

### New

- `LastUpdatedAt` added to `BaseEntity`, defaulting to `GETUTCDATE()` at the database level via entity configuration. Populated on insert; update-time stamping (per-service vs. centralized in `AppDbContext.SaveChangesAsync`) still to be decided.
- Logging (`Log` entity, in progress) — inherits `BaseEntity` for consistency with the rest of the domain, though `LastUpdatedAt` is not meaningful for log rows and is left unused.

---

## Tech Stack

- ASP.NET Core Web API (C#)
- Entity Framework Core (SQL Server, code-first)
- `Microsoft.Extensions.DependencyInjection`

## NuGet Packages

```
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
Microsoft.EntityFrameworkCore.Design
Microsoft.AspNetCore.Authentication.JwtBearer
Microsoft.AspNetCore.OpenApi
Swashbuckle.AspNetCore.Filters
Swashbuckle.AspNetCore
```

---

## Folder Structure

```
/Models
    BaseEntity.cs
    User.cs
    Post.cs
    Comment.cs
    Friendship.cs
    FriendshipStatus.cs
    Message.cs
    Log.cs
    RefreshToken.cs
/Data
    AppDbContext.cs
    /Configurations
        UserConfiguration.cs
        PostConfiguration.cs
        CommentConfiguration.cs
        FriendshipConfiguration.cs
        MessageConfiguration.cs
        LogConfiguration.cs
        RefreshTokenConfiguration.cs
    /Repositories
        /Base
            BaseRepository.cs
            BaseEntityRepository.cs
        UserRepository.cs
        PostRepository.cs
        CommentRepository.cs
        FriendshipRepository.cs
        MessageRepository.cs
        LogRepository.cs
        RefreshTokenRepository.cs
/BusinessLogic
    /Mappers
    /Services
        /Main
            AuthService.cs
            AccountService.cs
            PostService.cs
            CommentService.cs
            MessageService.cs
            FriendshipService.cs
            LogService.cs
        /Helpers
            TokenHelper.cs
            PasswordHasher.cs
        /Logging
            SystemLogger.cs
           
/Common
    /Dtos
        /Auth
        /Comments
        /Common
        /Messages
        /Posts
        /Users
    /Responses
        ApplicationResponse.cs
        ApplicationResponse<T>.cs : ApplicationResponse
        ListResponse<T>.cs : ApplicationResponse
/Controllers
    /Base
        BaseController.cs
    AuthController.cs
    UsersController.cs
    PostsController.cs
    CommentsController.cs
    FriendshipsController.cs
    MessagesController.cs
/ProjectConstants
    Constants.cs
Program.cs
```

---

## Architecture

```
Models
Data (DbContext + Configurations + Repositories)
BusinessLogic (Dtos, Mappers, Responses, Services)
Controllers
Program.cs
```

### Layers

**Models** — plain entity classes and enums, unchanged from the console app.

**Repositories** — concrete classes, no interfaces. `BaseRepository<T>` and `BaseEntityRepository<T>` provide shared query, pagination, and transaction logic. Specific repositories extend these with domain queries. Style unchanged from the console app.

**Services** — business logic. Consume repositories and mappers directly (no one-class-per-operation split at this stage — the existing per-entity services are not complex enough to justify it). Return `ApplicationResponse<T>` / `ApplicationResponse` when failure states are possible; raw values or `void` otherwise.

**Controllers** — new layer, replacing menus. Thin; delegate to services and map `ApplicationResponse` outcomes to HTTP status codes.

---

## Coding Style

- **Controllers** — early returns are used freely for validation/failure branching, in line with typical ASP.NET Core controller conventions.
- **Everywhere else** (services, repositories, mappers) — the console app's style is retained: no early returns, single `return` at the end of a method, branching via `if`/`else`.

Other retained conventions:

- No interfaces — concrete repository, service, and mapper classes only.
- `BaseRepository<T>` / `BaseEntityRepository<T>` split — composite-key entities (e.g. `Friendship`) share base methods without forcing an integer PK.
- `Query()` virtual override per repository for default `Include` chains. (moving away from this and using manual includes per business operation as they become different as they become more and more specific)
- `DeleteWhereAsync` uses `ExecuteDeleteAsync`, bypassing the change tracker, for bulk cleanup.
- `ApplicationResponse` wrapper omitted where no failure state is possible.

---

## Repository Layer

> **Carried over from the console app almost verbatim — method list should be accurate, but worth a pass to confirm nothing changed during the paste.**

### `BaseRepository<T> where T : class`

- `Query()` — `protected virtual IQueryable<T>`; override to apply default `Include` chains
- `GetAllAsync(page? pageSize?)` - simple method to fetch all data with optional pagination
- `GetWhereAsync(predicate?, page?, pageSize?, orderBy?, track?)` — composes the three steps; `predicate` is now optional (was required), so pagination-only or order-only calls don't need a no-op filter
- `GetFirstAsync(predicate)` — returns `T?`
- `AddAsync(T entity)`
- `DeleteAsync(T entity)`
- `DeleteWhereAsync(predicate)` — uses `ExecuteDeleteAsync`; bypasses change tracker
- `ExecuteInTransactionAsync(Func<Task> operation)` — wraps operations in a DB transaction; safe to call from any repository since all share the same scoped `DbContext`
- `ClearTracker()` — detaches all tracked entities; used before bulk delete sequences to avoid change tracker conflicts

### `BaseEntityRepository<T> where T : BaseEntity`

Extends `BaseRepository<T>`. Adds:

- `GetByIdAsync(int id)`
- `FindAsync(int id)` — uses `FindAsync` directly without includes; used when a clean untracked instance is needed
- `ExistsAsync(int id)`
- `DeleteAsync(int id)`

### Pagination

`GetWhereAsync` accepts optional `page` and `pageSize`. `Skip`/`Take` is applied and translated to SQL when both are provided. In the API, these values come from query parameters rather than the console app's `PaginatedInput`.
`GetAllAsync` also accepts optional `page` and `pageSize` parameters, applied accordingly.

### SaveChanges Strategy

`SaveChangesAsync` is called in the repository. For atomic multistep operations, wrap in a transaction via `ExecuteInTransactionAsync` at the service layer. `ExecuteDeleteAsync` bypasses the change tracker and does not require `SaveChangesAsync`.

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

### `PageQuery`

Shared query-binding DTO for paginated list endpoints, in `/Common/Dtos/Common/`:

```csharp
public class PageQuery
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
```

Bound via `[FromQuery]`. Properties are defaulted, deliberately: several service methods (e.g. `GetFeedAsync`) treat `null` page/size as "no pagination, return everything," a mode used by internal/non-API callers (cascade cleanup, seeding). Controllers unpack `PageQuery` into the individual `page`/`pageSize` primitives before calling a service — the service layer does not take a dependency on `PageQuery` itself. No size clamping/validation yet.

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

---

## DI Setup


```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>();

// AddScoped per repository

// AddScoped per mapper

// AddScoped per service
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<FriendshipService>();
builder.Services.AddScoped<TokenHelper>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<SystemLogger>();

builder.Services.AddControllers();

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        // ...
    });
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication(); // must precede UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
```

`ValidateIssuer`/`ValidateAudience` are deliberately `false` for now — single-client setup, no immediate need to distinguish token audiences. claims are read from `HttpContext.User` per request, no DI registration needed for that.

---

## Entities

| Entity     | Properties                                                                                    |
|------------|------------------------------------------------------------------------------------------------|
| User       | Username, Email, PasswordHash, PasswordSalt, Bio, DateOfBirth                                  |
| Post       | UserId, Content                                                                                |
| Comment    | UserId, PostId, Content                                                                        |
| Friendship | RequesterId, AddresseeId, Status (enum: Pending/Accepted/Declined), CreatedAt                  |
| Message    | SenderId, ReceiverId, MessageContent, IsRead, SentAt                                           |
| Log        | *(in progress)* — inherits `BaseEntity`; `LastUpdatedAt` present but unused                    |
| RefreshToken | UserId (FK, cascade delete), TokenHash (MaxLength 44, unique-indexed), ExpiresAt, RevokedAt (nullable) — inherits `BaseEntity`; `LastUpdatedAt` |

`BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- `Friendship` and `Message` do not inherit `BaseEntity` — `Friendship`'s PK is `(RequesterId, AddresseeId)`.
- `FriendshipStatus` is stored as a string in the database.
- `User.DateOfBirth` is validated via check constraint. Age must be between 13 and 130.

### FK Cascade Behavior

Unchanged from the console app. SQL Server disallows multiple cascade paths, so only `User → Posts` and `Post → Comments` use `Cascade`; all other relationships use `Restrict` with manual cleanup on account deletion.

---

## Authentication & Authorization

Password hashing/salting is retained from the console app. JWT access tokens + rotating refresh tokens are implemented. Role/permission scheme not yet implemented — deferred; ownership-style checks (e.g. can only delete own comment) continue to be handled in services against `HttpContext.User`'s claims rather than through roles, since most authorization needs so far are ownership checks, not role checks.

### Access tokens

- `TokenHelper.GenerateToken(userId, username)` issues a JWT signed with `HmacSha512Signature`, claims: `NameIdentifier`, `Name`.
- Lifetime controlled by `Jwt:AccessTokenMinutes` in config; validation side (`AddJwtBearer` in `Program.cs`) reads `Jwt:Issuer`/`Jwt:Audience`/`Jwt:Key`, `ValidateIssuer`/`ValidateAudience` currently `false` (single-client setup), `ValidateLifetime` true, `ClockSkew` tightened to 1 minute.
- Delivered to the client in the JSON response body, attached by the client as `Authorization: Bearer {token}`. Necessarily body-delivered rather than cookie-delivered, since Bearer auth requires client-side JS to read and attach it.

### Refresh tokens

- `RefreshToken : BaseEntity` — `UserId` (FK to `User`, `OnDelete(Cascade)`, no `User` collection nav property — only `RefreshToken.User` many-to-one, added only where an `Include` is actually needed), `TokenHash` (`MaxLength(44)`, unique-indexed — SHA256 digest is a fixed 32 bytes / 44 base64 chars), `ExpiresAt`, nullable `RevokedAt` (`null` = active).
- `TokenHelper.GenerateRefreshToken()` — 64 random bytes via `RandomNumberGenerator` (not `Guid`, which has no documented cryptographic-randomness guarantee), base64-encoded. `TokenHelper.HashToken(token)` — SHA256, not PBKDF2/bcrypt: the token is high-entropy and machine-generated (not human-chosen/guessable), so a slow KDF buys nothing against brute-force here; hashing at rest still matters as a defense against DB-read/leak exposure, same reasoning as password hashing but against a different threat (leak, not guessing).
- `RefreshTokenRepository` — `GetActiveByHashAsync(tokenHash)` and `RevokeAllForUserAsync(userId)` query `_dbSet` directly (bypassing the `Query()` override's `Include(rt => rt.User)`) since neither needs the loaded `User` navigation. `RevokeAsync(token)` sets `RevokedAt` and saves.
- Delivered via `HttpOnly; Secure; SameSite=Strict` cookie, set/read/cleared in `AuthController` (cookie access is `HttpContext`-scoped, not a service concern). `Expires` mirrors `Jwt:RefreshTokenDays`; the DB-side `ExpiresAt` is the authoritative check, the cookie's `Expires` is browser-side hygiene only.
- Rotated on every refresh: old token revoked, new one issued. Revoked (not deleted) on logout, to preserve an audit trail.
- No FK/schema changes were needed on `User` — refresh tokens live entirely in their own table.

### Auth flow

1. **Login** (`POST /auth/login`) — validates credentials, issues access token (body) + refresh token (cookie) via a shared private `IssueTokensAsync` helper.
2. **Authenticated requests** — client attaches `Authorization: Bearer {accessToken}`.
3. **Refresh** (`POST /auth/refresh`) — no body needed; refresh token cookie is read automatically. Hashes the incoming token, looks it up, revokes it, issues a new pair via the same `IssueTokensAsync` helper.
4. **Logout** (`POST /auth/logout`) — revokes the current refresh token (if any) and clears the cookie.

### Error handling / response conventions

- Auth failure messages are deliberately generic and uniform: login doesn't distinguish "no such user" from "wrong password"; refresh/logout don't distinguish missing, invalid, expired, or revoked token. This avoids giving a caller an oracle into account existence or token state.
- Refresh/logout auth failures return `401`, not `400` — the request is well-formed, the credential just fails to authenticate.
- `LogoutAsync` returns `void` (no `ApplicationResponse` wrapper) — it has no failure state by design (always looks like success to the caller, whether or not the token was valid), consistent with the existing convention of omitting the wrapper when no failure state is possible.
- `RegisterAsync` returns `ApplicationResponse<UserDto>` with the created user (password hash/salt excluded), not a bare success/fail response — matches REST convention of returning the created resource. Registration does not implicitly log the user in (deferred; would be a small addition later via the existing `IssueTokensAsync` helper, not a rewrite).
- `SessionUser` and its conversion methods were removed — dead code once `LoginAsync` issues tokens instead of a session object.

### Global exception handling

- `UseExceptionHandler` + `SystemLogger` (file-based, environment-aware detail: full exception in `Development`, generic message otherwise).
- A `DbUpdateException` → `409 Conflict` branch (rest fall through to `500`) covers a narrow TOCTOU race: a user could be deleted between an existence check and a dependent write (e.g. during refresh-token reissuance in `RefreshAsync`). Accepted as a rare, safely-failing edge case — the FK constraint guarantees no orphaned data either way — rather than adding `Serializable`-isolation transactions everywhere to close a near-impossible timing window.

---

## Features

- Register and log in 
- Create, view, update, and delete posts
- Comment on posts; delete own comments
- Send, accept, decline, and cancel friend requests; view friends list; remove friends; browse/search users
- Send and view messages between friends; paginated conversation retrieval via query parameters
- View a user's profile, with friendship-status-aware data (mirrors the console app's context-aware profile actions, translated to API response shape)
- Delete account with full data cleanup (same cascade/cleanup order as the console app)
- Logging (in progress) — `ActionLog`-style table for tracking actions/errors

---

## Key Design Decisions

Carried over from the console app:

- No interfaces — concrete repository, service, and mapper classes only.
- `BaseRepository<T>` / `BaseEntityRepository<T>` split — composite PK entities (`Friendship`) share base methods without forcing an integer PK.
- `Query()` virtual override — specific repositories define their own default includes.
- `PostRepository.Query()` excludes comments — including them causes EF change tracker conflicts when deleting posts with `ExecuteDeleteAsync`.
- `DeleteWhereAsync` uses `ExecuteDeleteAsync` — bypasses change tracker; used for all bulk cleanup.
- `ExecuteInTransactionAsync` is public — callable from any injected repository in a service; safe because all repositories share the same scoped `DbContext`.
- `ClearTracker()` called before account deletion — prevents change tracker conflicts from navigation properties loaded earlier in the session. *(Guess: this may need re-checking — the console app had a long-lived session where entities accumulated in the tracker across many actions; a Web API's per-request scoped `DbContext` may not hit this problem the same way, so `ClearTracker()` might be less necessary here. Worth revisiting.)*
- `GetFriendsByConversationStatusAsync` in `UserRepository` — single method with a `bool hasConversation` flag to avoid duplication.
- `GetConversationAsync` fetches descending, reversed per page in service — page 1 shows most recent; within a page messages print oldest-to-newest.
- `DisplayMessageDto` has no `Id` — no per-message actions exist.
- `ApplicationResponse` wrapper omitted when no failure state is possible.
- No early returns outside controllers — single `return` at end of method; branching via `if`/`else`.

New to the API version:

- Controllers use early returns, unlike the rest of the codebase — matches typical ASP.NET Core convention and keeps validation/failure branching readable at the routing layer.
- `LastUpdatedAt` on `BaseEntity`, defaulted via `HasDefaultValueSql("GETUTCDATE()")` in configuration rather than relying on the C# property initializer, so existing/new rows get a DB-level default.
- JWT access tokens + rotating refresh tokens, `AuthController`, and global exception handling — see Authentication & Authorization above for full detail.
- `BaseController` (with `[ApiController]`, inherited by all derived controllers) centralizes `GetUserId()`, avoiding per-controller duplication.
- `PageQuery` (nullable `Page`/`PageSize`) standardizes list-endpoint pagination binding, without forcing the service layer to depend on an API-layer DTO.
- Controller-level `[Authorize]` with `[AllowAnonymous]` overrides, rather than tagging every action, used where most of a controller's actions require auth.

### Bugs debugged this session

- JWT claims appearing empty on endpoints without `[Authorize]` when testing via Swagger — not a server bug. Swagger's "Authorize" button only attaches the bearer token to operations it detects as requiring auth (via `SecurityRequirementsOperationFilter` reading `[Authorize]`); endpoints without it never receive the token from Swagger UI even after global authorization, though `HttpContext.User` is in fact populated correctly server-side.

---

## Status

Actively in progress. Domain and data layers are migrated. JWT access + refresh token authentication is implemented and manually tested end-to-end (login/refresh/logout all confirmed working). `PostsController` is complete (list/detail/feed/own/by-user/create/update/delete). Next: remaining controllers (Comments, Friendships, Messages), following the same patterns established in `PostsController` (`BaseController`, `PageQuery`, controller-level `[Authorize]` with `[AllowAnonymous]` overrides, service-scoped ownership checks). Logging (`Log`/`ActionLog`) to follow once controllers are done — the two aren't dependent on each other, but controllers take priority. Role/permission scheme and registration auto-login are deliberately deferred, not tracked as pending TODOs.