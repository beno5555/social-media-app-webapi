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

Actively in progress. Domain and data layers are migrated. JWT access + refresh token authentication is implemented and manually tested end-to-end (login/refresh/logout all confirmed working). `PostsController` is complete (list/detail/feed/own/by-user/create/update/delete). `CommentController` is complete (detail/feed/own/by-user/create/update/delete). Next: remaining controllers (Friendships, Messages), following the same patterns established in `PostsController` and `PostsController` (`BaseController`, `PageQuery`, controller-level `[Authorize]` with `[AllowAnonymous]` overrides, service-scoped ownership checks). Logging (`Log`/`ActionLog`) to follow once controllers are done — the two aren't dependent on each other, but controllers take priority. Role/permission scheme and registration auto-login are deliberately deferred, not tracked as pending TODOs.