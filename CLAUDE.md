# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**WebCity** is an ASP.NET Core Web API (`net10.0`) for a small social platform: posts, comments, friendships, direct messages, presence. It uses EF Core with SQL Server, JWT auth with rotating refresh tokens, SignalR, MailKit email and background cleanup jobs. A frontend is planned. This repo is about to become a monorepo (`backend/` + `frontend/`), so paths below will move. The restructure happens **inside** the current root folder: never rename or relocate the root itself, because Claude Code's project memory is keyed to its path.

## Commands

Run from the repo root (the folder that holds `aspnetproject.slnx`):

```bash
dotnet build aspnetproject.slnx
dotnet run --project aspnetproject                      # Swagger UI + OpenAPI only in Development
dotnet test aspnetproject.IntegrationTests
dotnet test aspnetproject.IntegrationTests --filter "FullyQualifiedName~PostControllerTests"
dotnet test aspnetproject.IntegrationTests --filter "FullyQualifiedName~PostControllerTests.GetById_ForNonexistentPost_ReturnsNotFound"

# Migrations (requires the dotnet-ef tool). Never hand-edit Migrations/ or the model snapshot.
dotnet ef migrations add <Name> --project aspnetproject
dotnet ef database update --project aspnetproject
```

No linter or formatter is configured.

### Config prerequisites

`appsettings.json`, `appsettings.Development.json` and `appsettings.Testing.json` are all **gitignored**, and the repo has no example template. A fresh clone will not run until they exist. The settings they need: `ConnectionStrings:DefaultConnection`, `Jwt:{Key,AccessTokenMinutes,RefreshTokenDays}`, `EmailConfiguration:*`, `PasswordConfiguration:ResetTokenMinutes`, `CleanupConfiguration:{RefreshTokens,SoftDeletedUsers,OldLogs}`, `Cors:AllowedOrigins`. Never print or commit their values.

### Integration tests

- They run against a **real local SQL Server** database. `DatabaseFixture` hardcodes `Server=localhost;Database=TestSocialMediaWebApiDb;Trusted_Connection=True`. The app under test reads `appsettings.Testing.json`'s `DefaultConnection`, which must point at the same database.
- The fixture runs `MigrateAsync()` once. Each test class then calls `ResetAndSeedAsync` (Respawn wipes everything except `__EFMigrationsHistory`, then `Fixtures/TestDataSeeder` reseeds deterministic users and ids). Tests rely on those seeded ids, and comments in each test file describe the seed state.
- `CustomWebApplicationFactory` removes all `IHostedService`s. It does **not** replace `EmailSender`, so flows that send mail (deactivation, password reset, failed-register warning) send real SMTP email during tests.
- Test names follow `Method_Condition_ExpectedResult` and are grouped by endpoint.

## Architecture

All application code lives in the `aspnetproject/` project. Dependencies point one way: **Controllers / Hubs → Services → Repositories → `ApplicationDbContext`**. `Common/` (constants, `ResponseMessages`, enums, `ApplicationResponse`) is shared by every layer. Folders are grouped by domain area (`Users`, `Content`), and the same grouping repeats in each layer.

The important patterns, each of which spans several files:

- **No interfaces.** Repositories, services and mappers are concrete classes, registered directly in `Extensions/ApplicationServicesExtensions.cs`. `Program.cs` composes startup only through `Add*` extension methods in `Extensions/`.
- **Result wrapper.** Services that can fail return `ApplicationResponse` or `ApplicationResponse<T>` (`Common/Responses`). Controllers map `Succeeded` to HTTP status codes at each call site. Unexpected exceptions go to `GlobalExceptionHandler`, which writes ProblemDetails and logs to the file-based `SystemLogger`.
- **Two loggers.** Services inherit `BaseService` and call `LogResultAsync`, which writes `{Service}.{Method}` outcome rows to the `Log` table through `DatabaseLogger`. `SystemLogger` (files under `Logs/`) is for unhandled exceptions and background-job failures. Controllers never log.
- **Repositories.** `BaseRepository<T>` is for keyless or composite-key types like `UserRole`. `BaseEntityRepository<T>` is for `BaseEntity` types with an `Id`. Subclasses override `protected virtual Query(bool track = true)` to set default `Include`s. `ExecuteInTransactionAsync` can be called from any repository, because all repositories share the scoped `DbContext`. Bulk cleanup uses `ExecuteDeleteAsync`.
- **Account states.** A global query filter on `User` hides deactivated (`AccountDeactivatedAt`) and soft-deleted (`AccountDeletedAt`) users from normal reads. A background job hard-purges soft-deleted users past their retention window. `LastUpdatedAt` is nullable and stamped by services; it carries meaning (`Message.IsEdited`).
- **Real-time.** One hub (`MessageHub` at `hubs/messages`) carries messages, comment notifications and presence. Services push through `IHubContext` in the same method that mutates data, and hubs do no validation. `UserConnectionTracker` is a thread-safe singleton. The JWT is read from the query string for hub connections (`AuthenticationExtensions`).
- **Auth.** `BaseController` provides `GetUserId()`, `GetUsername()` and `IsAdministrator()`. `[Authorize]` is set per controller, with `[AllowAnonymous]` overrides. Nearly every action has `[EnableRateLimiting(RateLimitConfig.Policies.X)]`: limits are per-IP before login and per-user after.
- **Frontend integration points.** CORS policy `OriginPolicy` takes its origins from `Cors:AllowedOrigins` (with credentials). Password-reset redirect URLs are checked against a **hardcoded** allowlist in `Infrastructure/Services/Helpers/UrlSecurityService.cs` (currently `http://localhost:5500`).
- **DTO tiers.** Output DTOs come in Minimal, Standard and Full tiers by audience, built by static mappers in `Infrastructure/Mappers`. Entities are never returned.

## Code style

Follow the project skill `.claude/skills/webcity-conventions` (it overrides the global `coding-conventions` baseline). The most important rules:

- **Outside controllers and hubs:** a single `return` at the end of each method, the success path nested inside a positive `if`, and conditions extracted into named booleans. Controllers may return early, checking success first.
- One type per file. No magic literals: use `Constants`, `ResponseMessages` or options classes. UTC times. Always use braces.

## Docs

`README.md` covers history and design decisions. `aspnetproject/Markdowns/Chunked/*.md` (`auth`, `controllers`, `data`, `signalr`, `background-jobs`) holds per-area detail. Update the relevant doc when you change behavior it describes. Known drift: the README shows the CORS policy as `SignalRTestPolicy`, but the code uses `OriginPolicy`.

## Agent skills

### Issue tracker

Issues and specs live as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root, created lazily. See `docs/agents/domain.md`.
