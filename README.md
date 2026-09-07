# WebCity

An ASP.NET Core Web API, migrated from an earlier C# console application that simulated a basic social media platform. Core domain logic (entities, EF Core configuration, repositories, mappers, DTOs) carried over from the console app largely unchanged; the console-specific presentation layer was removed and replaced with a proper API layer.

---

## Migration Context

This project began as a console application using EF Core code-first, layered architecture, and manual DI. The console app's job was to build the domain and data layers correctly, with a hand-rolled console menu system standing in for a UI.

Moving to a Web API meant replacing the presentation layer, not the domain. The menu system was doing the same job controllers do — routing user actions to services and rendering results — but shaped around a single-user, stateful console session. That shape doesn't fit a stateless, multi-client API.

### Carried over as-is

- Entities and `BaseEntity`
- `ApplicationDbContext` and entity configurations
- Repositories (`BaseRepository<T>`, `BaseEntityRepository<T>`, and specific repositories) — concrete classes, no interfaces
- Mappers
- DTOs (base structure retained, expanded per endpoint as needed)
- `Constants.cs`

### Dropped

- `Printer`, `Prompter`, `DtoPrompter` — console I/O, no equivalent in an API
- All menus (`BaseMenu`, `MainMenu`, `UnauthenticatedMenu`, `AuthenticatedMenu`, `PostMenu`, `FriendMenu`, `MessageMenu`) — replaced by controllers
- `PaginatedInput`, `ConversationInput` — console input shapes; pagination now comes from query parameters
- `NavigateToRootException`, `AccountDeletedException` — solved stack-unwinding in a console menu loop; no equivalent in a stateless request/response cycle
- `SessionUser` in its original form — reworked around reading claims from `HttpContext.User` per request

### New since the initial migration

- `LastUpdatedAt` on `BaseEntity` is now `DateTime?` (nullable, defaults to `null`), stamped explicitly per-service on update rather than DB-defaulted — used as a signal in places (e.g. `Message.IsEdited => LastUpdatedAt is not null`), not just an audit column.
- Role-based authorization (`Role`, `UserRole`, `RoleName` enum: `User`/`Administrator`) — seeded via `HasData`, assignable/unassignable through `AccountManagementController`.
- Soft-delete + deactivation as two distinct account states, both hidden from normal queries via a global query filter on `User`, plus background jobs that purge deactivated-turned-deleted accounts past a retention window.
- Account activation flow (self-service reactivation via emailed token) — separate controller/service from deactivation.
- Password reset flow (`AuthController`: request + reset), backed by the same `ResetTokenHash`/`ResetTokenExpiresAt` fields used for account activation tokens.
- Real email delivery (`EmailSender`, MailKit/MimeKit) for security notifications (deactivation, reactivation, password reset, failed-register-attempt warning) — previously not present.
- Structured DB logging (`Log` entity + `DatabaseLogger`, `BaseService.LogResultAsync`) — implemented request-facing service methods log their outcomes; distinct from the file-based `SystemLogger` used for unhandled exceptions and background-job failures.
- Background hosted services (`PeriodicHostedService` base) for refresh-token cleanup, soft-deleted-user purging, and old-log pruning. See `background-jobs.md`.
- Rate limiting (`Microsoft.AspNetCore.RateLimiting`) — per-endpoint sliding-window policies, IP-based pre-auth (login/register/refresh/password-reset) and user-based post-auth, plus a global per-user fallback limiter.
- Presence tracking (`UserConnectionTracker`, `PresenceService`) — online/offline broadcast over SignalR, `User.LastOnlineAt` (`null` = online).
- Live comment notifications — `CommentService` pushes a `ReceiveComment` event over the same `MessageHub` used for messages.

---

## Tech Stack

- ASP.NET Core Web API (C#), target framework `net10.0`
- Entity Framework Core (SQL Server, code-first)
- SignalR (real-time push: messages, comments, presence)
- ASP.NET Core Rate Limiting middleware
- MailKit / MimeKit (SMTP email delivery)
- Bogus - Test data generation

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
MailKit
Bogus
```

---

## Folder Structure

```
/Data
    ApplicationDbContext.cs
    /Models
        BaseEntity.cs
        User.cs
        Post.cs
        Comment.cs
        Friendship.cs
        FriendshipStatus.cs   (moved under Common/ProjectConstants/Enums)
        Message.cs
        Log.cs
        RefreshToken.cs
        Role.cs
        UserRole.cs
    /Configurations
        UserConfiguration.cs
        ...
    /Repositories
        /Base
            BaseRepository.cs
            BaseEntityRepository.cs
        /Dtos
            ConversationFriendProjection.cs
        UserRepository.cs
        ...
/Common
    /Attributes/DataValidation
        ValidAgeAttribute.cs
    /ProjectConstants
        Constants.cs
        ResponseMessages.cs
        /Enums
            FriendshipStatus.cs
            RoleName.cs
    /Responses
        ApplicationResponse.cs (+ generic ApplicationResponse<T>)
        ListResponse.cs
/Infrastructure
    /Dtos
        /Users
        ...
    /Mappers
        UserMapper.cs
        ...
    /Queries
        PageQuery.cs, SearchUserQuery.cs
    /Services
        /BusinessLogic
            /Base
            /Content
            /Users
            LogService.cs (registered placeholder; no public behavior yet)
        /Helpers
        /Logging
        /Websockets
        /BackgroundJobs
            /Base 
            /Common
            /Configuration  
            OldLogsCleanupService.cs, RefreshTokenCleanupService.cs, SoftDeletedUsersCleanupService.cs
/Controllers
    /Base
    /Content
    /Users
/Hubs
    MessageHub.cs
/Extensions
    ApplicationServicesExtensions.cs, AuthenticationExtensions.cs,
    CorsExtensions.cs, GlobalExceptionHandler.cs, RateLimitConfig.cs, SwaggerExtensions.cs
/Markdowns
Program.cs
```

Note: the on-disk namespace/folder layout has drifted from `ProjectConstants`-only enums — `FriendshipStatus` now lives under `Common/ProjectConstants/Enums/`, not as a top-level model file. `AuthController`, `UserController`, `FriendshipController`, `AccountManagementController`, and `AccountActivationController` all live under `Controllers/Users/`, mirroring the `BusinessLogic/Users` service grouping — despite auth/friendships not being literally about "users" as a resource, they group by domain area rather than by REST noun.

---

## Architecture

```
Data (Models + Configurations + Repositories)
Common (Constants, Enums, Attributes, Responses)
Infrastructure (Dtos, Mappers, Queries, Services: BusinessLogic / Helpers / Logging / Websockets / BackgroundJobs)
Controllers
Hubs
Program.cs
```

### Layers

**Models** — plain entity classes and enums, largely unchanged from the console app; extended with `Role`/`UserRole` and account-lifecycle fields on `User`.

**Repositories** — concrete classes, no interfaces. `BaseRepository<T>` and `BaseEntityRepository<T>` provide shared query, pagination, and transaction logic. Specific repositories extend these with domain queries.

**Services** — business logic. Implemented request-facing services inherit `BaseService`, which injects `DatabaseLogger` and provides `LogResultAsync` for writing structured outcome logs. Consume repositories and mappers directly (no one-class-per-operation split). Return `ApplicationResponse<T>` / `ApplicationResponse` when failure states are possible; raw values or `void` otherwise. `LogService` is registered but currently only holds `ApplicationDbContext`; it has no exposed behavior yet.

**Controllers** — thin; delegate to services and map `ApplicationResponse` outcomes to HTTP status codes. All routes are under `api/` (e.g. `api/posts`, `api/auth`).

**Hubs** — SignalR transport layer, its own tier alongside Controllers, not a controller and not a service.

---

## Coding Style

- **Controllers** — early returns used freely for validation/failure branching, in line with typical ASP.NET Core controller conventions.
- **Everywhere else** (services, repositories, mappers) — no early returns, single `return` at the end of a method, branching via `if`/`else`.

Other retained conventions:

- No interfaces — concrete repository, service, and mapper classes only.
- `BaseRepository<T>` / `BaseEntityRepository<T>` split — non-`BaseEntity` types (`UserRole`, composite/no-single-PK style entities) use `BaseRepository<T>` directly; entities with an `Id` use `BaseEntityRepository<T>`.
- `Query()` is now a `protected virtual IQueryable<T> Query(bool track = true)` — takes an explicit tracking flag instead of relying on a separate no-tracking path; specific repositories override it for default `Include` chains.
- `DeleteWhereAsync` uses `ExecuteDeleteAsync`, bypassing the change tracker, for bulk cleanup.
- `ApplicationResponse` wrapper omitted where no failure state is possible (`LogoutAsync`, `MarkAsReadAsync`, `GetUnreadConversationsCount`).

---

## DI Setup

`Program.cs` composes everything through extension methods rather than one large block:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSwaggerConfiguration();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddApplicationRateLimiting();
builder.Services.AddCorsPolicies(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplicationServices(builder.Configuration); // repositories, services, SignalR, background jobs, email config

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapSwagger();
    app.MapSwaggerUI();
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("SignalRTestPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<MessageHub>("hubs/messages");

app.Run();
```

`AddApplicationServices` (in `Extensions/ApplicationServicesExtensions.cs`) registers: `AddSignalR()`, the singleton `UserConnectionTracker`, every repository (`AddScoped`), every business-logic service (`AddScoped`), `PasswordHasher`/`TokenGenerator` (`AddScoped`), `SystemLogger`/`DatabaseLogger` (`AddScoped`), `EmailConfiguration` options + `EmailSender` (`AddTransient`), the three background-job `IOptions<T>` configurations, and the three `AddHostedService<...>()` registrations.

`ValidateIssuer`/`ValidateAudience` remain `false` — single-client setup. JWT validation and the SignalR query-string token handoff both live in `AuthenticationExtensions.AddJwtAuthentication`.

---

## Features

- Register and log in; JWT access + rotating refresh tokens; password reset via emailed token
- Create, view, update, and delete posts
- Comment on posts; delete own comment, or (as post author or admin) someone else's; live `ReceiveComment` push to the post author
- Send, accept, decline, and remove friend requests; view friends/pending/sent; browse/search users
- Send, edit (time-windowed), and delete messages between friends; live push (`ReceiveMessage`, `MessageEdited`, `MessageSeen`) over SignalR; paginated conversation retrieval
- Presence: online/offline broadcast to friends via SignalR when a user's first/last connection opens/closes
- View a user's profile; role-aware access (`Administrator` role can update/delete/deactivate/reactivate other users' accounts)
- Deactivate/reactivate own or (as admin) another account; self-service reactivation via emailed activation token
- Soft-delete own or (as admin) another account; background job purges soft-deleted accounts (and their messages/friendships/posts) past a retention window
- Structured DB action logging for implemented request-facing service calls (`Log` table) plus file-based system/error logging
- Rate limiting on every mutating and most read endpoints
- Admin role assignment/removal

---

## Key Design Decisions

Carried over from the console app:

- No interfaces — concrete repository, service, and mapper classes only.
- `ExecuteInTransactionAsync` — callable from any injected repository in a service; safe because all repositories share the same scoped `DbContext`.
- `ClearTracker()` (now on `BaseEntityRepository<T>`, not `BaseRepository<T>`) — called before soft-delete's related-data cleanup to avoid change-tracker conflicts.
- No early returns outside controllers.

New to the API version:

- `BaseService` centralizes `DatabaseLogger` injection and `LogResultAsync` — implemented request-facing business-logic services log success and/or failure paths per operation, tagged `{ServiceName}.{MethodName}`. The currently empty `LogService` is the exception.
- `BaseController` (`[ApiController]`, inherited by all derived controllers) centralizes `GetUserId()`, `GetUsername()`, and `IsAdministrator()` (role check via `User.IsInRole`).
- `PageQuery` (`PageNumber` defaults to `1`, `PageSize` defaults to `Constants.DefaultPageSize` with a `[Range(1, MaxPageSize)]` validation) standardizes list-endpoint pagination binding — no longer nullable/unvalidated. `SearchUserQuery : PageQuery` adds a validated `Username` filter.
- Controller-level `[Authorize]` with `[AllowAnonymous]` overrides, rather than tagging every action, used where most of a controller's actions require auth.
- `[EnableRateLimiting(RateLimitConfig.Policies.X)]` on nearly every action — see `controllers.md` per-controller, and `RateLimitConfig.cs` for the concrete limits (mostly per-user sliding windows; auth endpoints are per-IP since the caller isn't authenticated yet).
- Global query filter on `User` (`AccountDeactivatedAt == null && AccountDeletedAt == null` — see `data.md`) means most repository reads transparently exclude deactivated/deleted accounts without each call site needing to remember to filter.

---

## Status

Actively developed, beyond the original migration scope. Domain/data layers, JWT auth (access + refresh + password reset), account activation/deactivation/soft-delete, role-based authorization, all five resource controllers (Posts, Comments, Friendships, Messages, Users/Accounts), SignalR (messages + live comments + presence), structured DB logging, background cleanup jobs, and rate limiting are all implemented. See `controllers.md`, `data.md`, `auth.md`, `signalr.md`, and `background-jobs.md` for area-specific detail.
