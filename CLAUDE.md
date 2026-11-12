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

# Linux / cloud sessions: same tests against a throwaway SQL Server in Docker. Arguments pass through to dotnet test.
scripts/test-linux.sh
scripts/test-linux.sh --filter "FullyQualifiedName~PostControllerTests"

# Migrations (requires the dotnet-ef tool). Never hand-edit Migrations/ or the model snapshot.
dotnet ef migrations add <Name> --project aspnetproject
dotnet ef database update --project aspnetproject
```

No linter or formatter is configured.

### Config prerequisites

`appsettings.json`, `appsettings.Development.json` and `appsettings.Testing.json` are all **gitignored**, and the repo has no example template. A fresh clone will not run until they exist. The settings they need: `ConnectionStrings:DefaultConnection`, `Jwt:{Key,AccessTokenMinutes,RefreshTokenDays}`, `EmailConfiguration:*` (including `Enabled`, which defaults to true), `PasswordConfiguration:ResetTokenMinutes`, `CleanupConfiguration:{RefreshTokens,SoftDeletedUsers,OldLogs}`, `Cors:AllowedOrigins`. Never print or commit their values.

### Integration tests

- They run against a **real local SQL Server** database. `DatabaseFixture` defaults to `Server=localhost;Database=TestSocialMediaWebApiDb;Trusted_Connection=True`. The app under test reads `appsettings.Testing.json`'s `DefaultConnection`, which must point at the same database.
- **Linux and cloud sessions** (no Windows authentication, no settings files): run `scripts/test-linux.sh` instead of `dotnet test`. It starts Docker and a SQL Server container, generates a database password (kept in `~/.webcity-test/`) and a JWT key, writes a secret-free `appsettings.Testing.json` only if none exists, and passes the connection string to both the fixture and the app through the `ConnectionStrings__DefaultConnection` environment variable. It needs Docker and the .NET 10 SDK; a cloud session without a prepared environment installs the SDK first with `apt-get update && apt-get install -y dotnet-sdk-10.0`. It shares nothing with the Windows setup.
- The fixture runs `MigrateAsync()` once. Each test class then calls `ResetAndSeedAsync` (Respawn wipes everything except `__EFMigrationsHistory`, then `Fixtures/TestDataSeeder` reseeds deterministic users and ids). Tests rely on those seeded ids, and comments in each test file describe the seed state.
- `CustomWebApplicationFactory` removes all `IHostedService`s. It does not replace `EmailSender`; instead it forces `EmailConfiguration:Enabled` to false, so flows that send mail (deactivation, password reset, failed-register warning) run normally but open no SMTP connection.
- Test names follow `Method_Condition_ExpectedResult` and are grouped by endpoint.

## Architecture

All application code lives in the `aspnetproject/` project. Dependencies point one way: **Controllers / Hubs → Services → Repositories → `ApplicationDbContext`**. `Common/` (constants, `ResponseMessages`, enums, `ApplicationResponse`) is shared by every layer. Folders are grouped by domain area (`Users`, `Content`), and the same grouping repeats in each layer.

The important patterns, each of which spans several files:

- **No interfaces.** Repositories, services and mappers are concrete classes, registered directly in `Extensions/ApplicationServicesExtensions.cs`. `Program.cs` composes startup only through `Add*` extension methods in `Extensions/`.
- **Result wrapper.** Services that can fail return `ApplicationResponse` or `ApplicationResponse<T>` (`Common/Responses`). Controllers map `Succeeded` to HTTP status codes at each call site. Unexpected exceptions go to `GlobalExceptionHandler`, which logs to the file-based `SystemLogger` and writes the same envelope (`409` for a database update conflict, `500` otherwise; exception detail in the message only in Development). Rate-limit rejections (`429`) and refresh failures (`401`) use the envelope too. Clients therefore see two failure shapes from the application: the envelope, and the framework's validation `ProblemDetails`. Framework-level rejections (`401`/`403` from the auth middleware, an unmatched route's `404`) are bare status codes with no body. See `auth.md`.
- **Two loggers.** Services inherit `BaseService` and call `LogResultAsync`, which writes `{Service}.{Method}` outcome rows to the `Log` table through `DatabaseLogger`. `SystemLogger` (files under `Logs/`) is for unhandled exceptions and for failures to write a `Log` row. Background jobs log through `DatabaseLogger`. Controllers never log.
- **Repositories.** `BaseRepository<T>` is for keyless or composite-key types like `UserRole`. `BaseEntityRepository<T>` is for `BaseEntity` types with an `Id`. Subclasses override `protected virtual Query(bool track = true)` to set default `Include`s. `ExecuteInTransactionAsync` can be called from any repository, because all repositories share the scoped `DbContext`. Bulk cleanup uses `ExecuteDeleteAsync`.
- **Account states.** A global query filter on `User` hides deactivated (`AccountDeactivatedAt`) and soft-deleted (`AccountDeletedAt`) users from normal reads. A background job hard-purges soft-deleted users past their retention window. `LastUpdatedAt` is nullable and stamped by services; it carries meaning (`Message.IsEdited`).
- **Real-time.** One hub (`MessageHub` at `hubs/messages`) carries messages, comment notifications and presence. Services push through `IHubContext` in the same method that mutates data, and hubs do no validation. `UserConnectionTracker` is a thread-safe singleton. The JWT is read from the query string for hub connections (`AuthenticationExtensions`).
- **Auth.** `BaseController` provides `GetUserId()`, `GetUsername()` and `IsAdministrator()`. `[Authorize]` is set per controller, with `[AllowAnonymous]` overrides. Nearly every action has `[EnableRateLimiting(RateLimitConfig.Policies.X)]`: the design is per-IP before login and per-user after, but **currently every limit, including the global limiter, counts by IP** because `UseRateLimiter` runs before `UseAuthentication` (ticket 06 in `.scratch/backend-frontend-readiness/issues/` fixes this).
- **Frontend integration points.** CORS policy `OriginPolicy` takes its origins from `Cors:AllowedOrigins` (with credentials). Password-reset redirect URLs are checked by the injected `UrlSecurityService` (`Infrastructure/Services/Helpers`) against the same `Cors:AllowedOrigins` list, comparing scheme and authority case-insensitively; with no origins configured, every redirect URL is rejected. The integration test host replaces the first entry of that list with `CustomWebApplicationFactory.TrustedFrontendOrigin`.
- **DTO tiers.** Output DTOs come in Minimal, Standard and Full tiers by audience, built by static mappers in `Infrastructure/Mappers`. Entities are never returned.

## Code style

Follow the project skill `.claude/skills/webcity-conventions` (it overrides the global `coding-conventions` baseline). The most important rules:

- **Outside controllers and hubs:** a single `return` at the end of each method, the success path nested inside a positive `if`, and conditions extracted into named booleans. Controllers may return early, checking success first.
- One type per file. No magic literals: use `Constants`, `ResponseMessages` or options classes. UTC times. Always use braces.

## Docs

`README.md` covers history and design decisions. `aspnetproject/Markdowns/Chunked/*.md` (`auth`, `controllers`, `data`, `signalr`, `background-jobs`) holds per-area detail. Update the relevant doc when you change behavior it describes.

## Agent skills

Matt Pocock's engineering skills are committed under `.claude/skills/` so cloud sessions get them. The copies in `~/.claude/skills/` with the same names are **Windows directory junctions into this repo**, so there is one copy on disk and editing through either path changes both. Never replace a junction with a real folder, and remove one with `cmd /c rmdir <path>`, not `rm -rf`, which can delete the repo copy through the link. The global SessionStart hook (`~/.claude/hooks/sync-remote.sh`) warns if a junction becomes a separate copy.

### Issue tracker

Issues and specs live as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root, created lazily. See `docs/agents/domain.md`.
