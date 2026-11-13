# WebCity

WebCity is a small social platform: posts, comments, friendships, direct messages and presence. This repository holds its backend, an ASP.NET Core Web API (`net10.0`) in `backend/`, built on EF Core with SQL Server, JWT authentication with rotating refresh tokens, SignalR, MailKit email and background cleanup jobs.

For the earlier, API-only state of this repository and the history behind it, see the git tag `api-only`.

---

## Features

- Register and log in; JWT access tokens plus rotating refresh tokens; password reset via an emailed token
- Create, view, update and delete posts; a friends-only feed
- Comment on posts and edit own comments; delete a comment as its author, as the post's author, or as an administrator; live `ReceiveComment` push to the post's author
- Send, accept, decline and remove friend requests; view friends, pending and sent requests; search users by username
- Send, edit and delete messages between friends; live push (`ReceiveMessage`, `MessageEdited`, `MessageSeen`) over SignalR; paginated conversations and read receipts. A message can be edited for 20 minutes after it is sent, and the receiver sees the edit live
- Presence: `UserOnline` / `UserOffline` broadcast to friends when a user's first connection opens or last connection closes
- View a user's profile; edit own profile
- Roles (`User`, `Administrator`): an administrator can create, update, deactivate, reactivate and soft-delete other users' accounts, delete any post or comment, and assign or remove the `Administrator` role
- Admin reads (`api/admin`): an administrator reads the `Logs` table page by page, newest first, filtered by user, authorized or not, succeeded or not, entity, action and date range, and lists accounts in any state (active, deactivated, soft-deleted), filtered by state and username
- Deactivate own account; reactivate it through an emailed activation token
- Soft-delete own account; a background job later purges soft-deleted accounts and their messages, friendships and posts once a retention window has passed
- Outcome logging of service calls to the `Logs` table, plus a file log for unhandled exceptions
- Rate limiting: a named policy on every endpoint except logout, and a global limiter over everything

---

## Tech Stack

- ASP.NET Core Web API (C#), target framework `net10.0`
- Entity Framework Core (SQL Server, code-first migrations)
- JWT bearer authentication
- SignalR (real-time push: messages, comments, presence)
- ASP.NET Core rate limiting middleware
- MailKit (SMTP email delivery)
- Swashbuckle and `Microsoft.AspNetCore.OpenApi` (Swagger UI and OpenAPI, Development only)
- Bogus (bulk test-data generation, `Data/Seed/BulkDataSeed.cs`)
- Integration tests: xUnit, `Microsoft.AspNetCore.Mvc.Testing`, Respawn, against a real SQL Server database

### NuGet packages (`backend/aspnetproject/aspnetproject.csproj`)

```
Bogus
MailKit
Microsoft.AspNetCore.Authentication.JwtBearer
Microsoft.AspNetCore.OpenApi
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Design
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
Swashbuckle.AspNetCore
Swashbuckle.AspNetCore.Filters
System.Security.Cryptography.Pkcs
```

---

## Repository Layout

```
/backend
    aspnetproject.slnx
    /aspnetproject                    the API project
    /aspnetproject.IntegrationTests   integration tests
/docs
    /backend                          per-area docs: auth, controllers, data, signalr, background-jobs
    /adr                              architecture decision records
    /agents                           notes for coding agents
/scripts
    test-linux.sh                     runs the tests against a throwaway SQL Server in Docker
/.github/workflows
    tests.yml                         CI
GLOSSARY.md
CLAUDE.md
```

### The API project (`backend/aspnetproject/`)

```
/Common
    /Attributes/DataValidation
        ValidAgeAttribute.cs
    /Domain
        ApplicationResponse.cs
        ListResponse.cs
    /ProjectConstants
        Constants.cs
        ResponseMessages.cs
        /Enums
            AccountState.cs
            FriendshipStatus.cs
            RoleName.cs
/Controllers
    /Admin
        AdminController.cs
    /Base
        BaseController.cs
    /Content
        CommentController.cs, MessageController.cs, PostController.cs
    /Users
        AccountActivationController.cs, AccountManagementController.cs,
        AuthController.cs, FriendshipController.cs, UserController.cs
/Data
    ApplicationDbContext.cs
    /Configurations
        UserConfiguration.cs
        ...
    /Models
        BaseEntity.cs
        Comment.cs, Friendship.cs, Log.cs, Message.cs, Post.cs,
        RefreshToken.cs, Role.cs, User.cs, UserRole.cs
    /Repositories
        /Base
            BaseRepository.cs
            BaseEntityRepository.cs
        /Dtos
            ConversationFriendProjection.cs
        UserRepository.cs
        ...
    /Seed
        BulkDataSeed.cs
        DeterministicFixtureSeed.cs
/Extensions
    ApplicationServicesExtensions.cs, AuthenticationExtensions.cs, CorsExtensions.cs,
    GlobalExceptionHandler.cs, RateLimitConfig.cs, SwaggerExtensions.cs, TestSeedExtensions.cs
/Hubs
    MessageHub.cs
/Infrastructure
    /Dtos
        /Accounts, /Auth, /Comments, /DatabaseLogs, /Friendships,
        /Messages, /Posts, /UserRoles, /Users, /WebsocketsTransfer
    /Mappers
        UserMapper.cs
        ...
    /Queries
        AccountListQuery.cs, PageQuery.cs, SearchUserQuery.cs
    /Services
        /BackgroundJobs
            /Base
            /Common
            /Configuration
            OldLogsCleanupService.cs, RefreshTokenCleanupService.cs, SoftDeletedUsersCleanupService.cs
        /BusinessLogic
            /Base
            /Content
            /Users
            LogService.cs
        /Helpers
        /Logging
        /Websockets
/Migrations
Program.cs
```

Folders are grouped by domain area, not by REST noun, and the grouping repeats across layers: `AuthController`, `UserController`, `FriendshipController`, `AccountManagementController` and `AccountActivationController` all live under `Controllers/Users/`, mirroring `Services/BusinessLogic/Users/`. `AdminController` (`Controllers/Admin/`) is the administrator-only area; it calls `LogService` and `UserService` rather than a service of its own.

---

## Running It

Run the `dotnet` commands from `backend/`:

```bash
dotnet build aspnetproject.slnx
dotnet run --project aspnetproject
dotnet test aspnetproject.IntegrationTests
```

`appsettings.json`, `appsettings.Development.json` and `appsettings.Testing.json` are gitignored and there is no example template, so a fresh clone does not run until they exist in `backend/aspnetproject/`. The application reads `ConnectionStrings:DefaultConnection`, `Jwt`, `EmailConfiguration`, `PasswordConfiguration:ResetTokenMinutes`, `CleanupConfiguration:{OldLogs,RefreshTokens,SoftDeletedUsers}` and `Cors:AllowedOrigins`.

The integration tests need a real SQL Server database. On Linux, `scripts/test-linux.sh` (run from the repository root) starts one in Docker and runs the same tests; CI uses that script.

---

## Architecture

Dependencies point one way: **Controllers / Hubs → Services → Repositories → `ApplicationDbContext`**. `Common/` (constants, response messages, enums, `ApplicationResponse`) is shared by every layer.

**Models** (`Data/Models`): plain entity classes. Enums live in `Common/ProjectConstants/Enums`.

**Repositories**: concrete classes. `BaseRepository<T>` and `BaseEntityRepository<T>` provide the shared query, pagination and transaction logic; specific repositories add domain queries.

**Services**: business logic. The services under `BusinessLogic/Content` and `BusinessLogic/Users` inherit `BaseService` and consume repositories and mappers directly. `LogService` also inherits `BaseService` and serves the administrator's log read through `LogRepository`.

**Controllers**: thin. They delegate to services and map `ApplicationResponse` outcomes to HTTP status codes. All routes are under `api/` (for example `api/posts`, `api/auth`).

**Hubs**: the SignalR transport, a tier of its own beside the controllers. One hub, `MessageHub` at `hubs/messages`, carries messages, comment notifications and presence. It is push-only and does no validation.

**Background jobs**: three hosted services built on `PeriodicHostedService` clean up refresh tokens, soft-deleted users and old log rows.

### Startup

`Program.cs` composes startup through `Add*` extension methods from `Extensions/`. Condensed:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

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

builder.Services.AddApplicationServices(builder.Configuration);

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

app.UseCors("OriginPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<MessageHub>("hubs/messages");

app.Run();
```

`AddApplicationServices` (`Extensions/ApplicationServicesExtensions.cs`) registers SignalR, the singleton `UserConnectionTracker`, every repository and service (scoped), the two loggers (scoped), `EmailSender` (transient), the options classes for email and the three cleanup jobs, and the three hosted services.

---

## Design Decisions

- **No interfaces.** Repositories, services and mappers are concrete classes, registered directly. Mappers are static.
- **Result wrapper.** A service method that can fail returns `ApplicationResponse` or `ApplicationResponse<T>`; the controller maps `Succeeded` to a status code. Where no failure state is possible the wrapper is left out (`AuthService.LogoutAsync`, `MessageService.MarkAsReadAsync`, `MessageService.GetUnreadConversationsCount`). Unhandled exceptions go to `GlobalExceptionHandler`, which writes the same envelope.
- **Control flow.** The house rule outside controllers and hubs is no early returns: a method has a single `return` at the end and branches with `if`/`else`. Controllers return early freely.
- **Two repository bases.** `BaseEntityRepository<T>` is for `BaseEntity` types and adds id-based operations. `UserRoleRepository` (composite key, no `Id`) and `FriendshipRepository` extend `BaseRepository<T>` directly. Subclasses override `protected virtual IQueryable<T> Query(bool track = true)` to set default `Include`s.
- **Transactions.** `ExecuteInTransactionAsync` can be called from any repository a service holds, because all repositories share the scoped `DbContext`.
- **Two loggers.** `BaseService` holds the `DatabaseLogger` and provides `LogResultAsync`, which writes an outcome row tagged `{ServiceName}.{MethodName}` to the `Logs` table. `SystemLogger` appends to `Logs/ErrorLogs.txt` for unhandled exceptions and for failures to write a log row. Background jobs log through `DatabaseLogger` too. Controllers do not log.
- **Account states.** Deactivation and soft-delete are two distinct states. A global query filter on `User` hides both from normal reads, so call sites do not have to remember to filter.
- **`LastUpdatedAt` carries meaning.** It is nullable and stamped by services on update, not defaulted by the database: `Message.IsEdited` is `LastUpdatedAt is not null`.
- **Mutate and notify together.** Services push SignalR events through `IHubContext` in the same method that changes the data.
- **Authorization.** `[Authorize]` is set per controller, with `[AllowAnonymous]` on public actions. `BaseController` provides `GetUserId()`, `GetUsername()` and `IsAdministrator()`. `AuthController` is the exception: every action is public, so it has no `[Authorize]` and does not inherit `BaseController`. Ownership checks are made in services.
- **Pagination.** List endpoints bind `PageQuery` from the query string: `PageNumber` defaults to 1, and `PageSize` defaults to 10 and is validated to 1–100.
- **Rate limiting.** Sliding-window policies, designed as per-IP before login and per-user after, with a global limiter as a backstop. Today every limit counts by IP, because the limiter runs before authentication (see `docs/backend/auth.md`).

---

## Docs

Per-area detail lives in `docs/backend/`:

- `auth.md`: authentication, tokens, account activation, roles, error shapes, rate limiting
- `controllers.md`: every endpoint, per controller
- `data.md`: repositories, entities, cascade behavior
- `signalr.md`: the hub, presence, events, CORS
- `background-jobs.md`: the cleanup jobs

Architecture decision records are in `docs/adr/`, and domain terms in `GLOSSARY.md`.
