# WebCity — Authentication & Authorization

> Part of the WebCity doc set. See also: `README.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

---

## Authentication & Authorization

Password hashing is PBKDF2 (`Rfc2898DeriveBytes.Pbkdf2`, 100,000 iterations, SHA-256, 32-byte salt and hash, both stored base64) — replaced the console app's original hashing scheme (`PasswordHasher`). JWT access tokens + rotating refresh tokens are implemented. Role-based authorization now exists (`User`/`Administrator`, via `Role`/`UserRole`); ownership-style checks (e.g. can only delete own comment, or a post author/admin can delete any comment on their post) continue to be handled in services against claims, alongside role checks for admin-only actions.

### Access tokens

- `TokenGenerator.GenerateAccessToken(user)` issues a JWT signed with `HmacSha512Signature`, claims: `NameIdentifier`, `Name`, plus one `ClaimTypes.Role` claim per role the user holds (`user.UserRoles`).
- Lifetime controlled by `Jwt:AccessTokenMinutes`; validation side (`AuthenticationExtensions.AddJwtAuthentication`) reads `Jwt:Key`, `ValidateIssuer`/`ValidateAudience` currently `false` (single-client setup), `ValidateLifetime` true, `ClockSkew` 1 minute.
- Delivered to the client in the JSON response body, attached by the client as `Authorization: Bearer {token}`.

### Refresh tokens

- `RefreshToken : BaseEntity` — `UserId` (FK, cascade delete), `TokenHash` (`MaxLength(44)`, unique-indexed — SHA-256 digest, 44 base64 chars), `ExpiresAt`, nullable `RevokedAt` (`null` = active).
- `TokenGenerator.GenerateRefreshToken()` — 64 random bytes via `RandomNumberGenerator`, URL-safe base64-encoded (`+`/`/` replaced, padding trimmed). `TokenGenerator.HashToken(token)` — SHA-256.
- `RefreshTokenRepository.GetActiveByHashAsync(hash)`, `RevokeAsync(token)`, `RevokeAllForUserAsync(userId)` (bulk `ExecuteUpdateAsync`, used on password reset, deactivation, and soft-delete).
- Delivered via `HttpOnly; Secure; SameSite=Strict` cookie, set/read/cleared in `AuthController`. `Expires` mirrors `Jwt:RefreshTokenDays`.
- Rotated on every refresh: old token revoked, new one issued via the shared `AccountSecurityService.IssueAccessAndRefreshTokensAsync` helper. Revoked (not deleted) on logout; a background job (`RefreshTokenCleanupService`) later purges rows that are both expired and revoked — see `background-jobs.md`.

### Auth flow

1. **Register** (`POST api/auth/register`) — creates the user (no roles beyond none assigned at registration; roles are assigned separately by an admin). Fails on duplicate email or username. On duplicate email, triggers `AccountSecurityService.HandleFailedRegisterAttempt` — emails the existing account holder a password-reset token, in case the registration attempt was the real owner locked out of their account.
2. **Login** (`POST api/auth/login`) — validates credentials, issues access token (body) + refresh token (cookie) via `AccountSecurityService.IssueAccessAndRefreshTokensAsync`.
3. **Authenticated requests** — client attaches `Authorization: Bearer {accessToken}`.
4. **Refresh** (`POST api/auth/refresh`) — no body; refresh token cookie read automatically, hashed, looked up, revoked, new pair issued. A missing, invalid, expired or revoked token returns `401` with the `ApplicationResponse` envelope (`succeeded: false` plus a message).
5. **Logout** (`PATCH api/auth/logout`) — revokes the current refresh token (if any) and clears the cookie.
6. **Password reset** — `PUT api/auth/request-password-reset` issues a reset token (reusing `User.ResetTokenHash`/`ResetTokenExpiresAt`) and emails it; `PUT api/auth/reset-password` consumes it, rejects if the new password equals the current one, revokes all the user's refresh tokens (forces re-login everywhere), and emails a "your password was reset" notice.

#### Reset return address (`RedirectUrl`)

The body of `request-password-reset` (`ForgotPasswordDto`) is `Email` plus an optional `RedirectUrl`, the page of the app that hosts the reset form.

- **Omitted (`null`):** the email carries the raw reset token and no link. The flow works without a link.
- **Provided:** the email carries a link `{RedirectUrl}?token={token}`, but only if `UrlSecurityService.IsAllowedRedirect` accepts it. Any provided value, including an empty string, goes through the check. A rejected value fails the request with `400` and the `Url origin not allowed` message, and no token is issued or emailed. The check runs only once the email matches an account; an unknown email fails with `400` and the user-not-found message whatever the `RedirectUrl`.
- **The check** reads `Cors:AllowedOrigins`, the same list that drives the `OriginPolicy` CORS policy, so the two cannot disagree. `RedirectUrl` must be an absolute URL whose scheme and authority (host plus any non-default port) equal an allowed origin, compared case-insensitively. Same host on another port or scheme is rejected. Only the origin is compared; the path and query of `RedirectUrl` are not checked. With no origins configured, every `RedirectUrl` is rejected (omitting it still works).
- `UrlSecurityService` is a registered concrete class, injected into `AuthService`. The integration test host replaces the first allowed origin with `CustomWebApplicationFactory.TrustedFrontendOrigin`.

### Account activation / deactivation (separate from auth, own controller/service)

- **Deactivation** (`AccountActivationController`/`AccountActivationService`) — sets `AccountDeactivatedAt`, revokes all refresh tokens immediately, emails the user. Self-service (`PATCH api/accounts/mine/deactivate`) and admin-on-other-user (`PATCH api/accounts/{id}/deactivate`) are separate endpoints/rate-limit policies but call the same service method.
- **Reactivation** — two paths: admin-initiated (`PATCH api/accounts/{id}/activate`, `Administrator`-only, immediate) and self-service via emailed token (`PATCH api/accounts/request-activate` to request the token, `PATCH api/accounts/activate` with the token in the body to confirm — both `[AllowAnonymous]`, since a deactivated user can't authenticate).
- Both activation-token issuance and password-reset-token issuance share `User.ResetTokenHash`/`ResetTokenExpiresAt` — different service methods (`HandleAccountActivationRequest` vs `HandlePasswordResetRequest`), same underlying fields, cleared on successful use.

### Roles

- Seeded via `RoleConfiguration.HasData`: `User` (Id 1), `Administrator` (Id 2). A user row (`UserConfiguration`, Id 1, `sandro_beno`) is also seeded with both roles (`UserRoleConfiguration`, `UserId = 1`). `DeterministicFixtureSeed` additionally seeds fixture users (Id 2 onward), friendships, posts, comments, messages and refresh tokens through `HasData`, so they are part of the migrations; the integration tests depend on them.
- `AccountManagementController` exposes `Administrator`-only assign/unassign endpoints (`PUT api/accounts/{id}/assign-administrator`, `PUT api/accounts/{id}/remove-administrator`).
- `BaseController.IsAdministrator()` checks `User.IsInRole(nameof(RoleName.Administrator))`; used by `PostController`/`CommentController` to let an admin delete any post/comment, and by `AccountManagementController`/`AccountActivationController` for admin-vs-self routes.
- No self-registration path grants roles — a new user has none until an admin assigns one.

### Error handling / response conventions

#### Failure shapes a client can receive

Two, and a client error handler needs only these:

1. **The envelope** — `ApplicationResponse` as JSON, `{ "succeeded": false, "message": "..." }`. Every failure that is not a validation failure uses it: service failures (login, register, password reset, ...), a failed or cookie-less refresh (`401`), an unhandled exception (`500`, or `409` for a database update conflict) and a rate-limit rejection (`429`). The human-readable text is always in `message`.
2. **Validation errors** — the framework's automatic `400` from `[ApiController]` model validation, a standard `ValidationProblemDetails` (`application/problem+json`) body with an `errors` map of field name to messages. Deliberately left in its standard shape so forms can show errors next to fields.

Framework-level rejections are not produced by the application and carry no body: a missing or invalid JWT (`401`), an authenticated caller with the wrong role (`403`), and a request to an unmatched route (`404`). Clients branch on the status code alone for these.

#### Other conventions

- Auth failure messages are deliberately generic and uniform: login doesn't distinguish "no such user" from "wrong password."
- `LogoutAsync` returns `void` — no failure state by design.
- `RegisterAsync` returns `ApplicationResponse<FullUserDto>` with the created user, matching REST convention of returning the created resource. Registration does not implicitly log the user in.
- Every `AuthService`/`AccountActivationService`/`AccountManagementService` method logs its outcome via `BaseService.LogResultAsync`, in addition to returning an `ApplicationResponse`.

### Global exception handling

- `GlobalExceptionHandler : IExceptionHandler` (registered via `AddExceptionHandler<T>()` + `AddProblemDetails()`) logs the exception through `SystemLogger.LogEndpointErrorAsync`, then maps `DbUpdateException` → `409 Conflict` (everything else → `500`), writing an `ApplicationResponse` envelope as the body. The message carries `ExceptionType: exception message | Inner: inner exception message` (the inner part only when there is one) only in `Development` and a generic message otherwise (no stack trace is returned).
- `SystemLogger` appends to a single text file (`Logs/ErrorLogs.txt` under the content root, not rotated) under a `SemaphoreSlim`-guarded async write — separate from the structured `DatabaseLogger` (DB-backed, per-action) used everywhere else.

### Rate limiting

`UseRateLimiter` runs after `UseAuthentication` and before `UseAuthorization` (`Program.cs`). Authentication has already read the access token when a partition key is computed, so per-user policies (`PerUserSlidingWindow`) and the global limiter give each signed-in user their own budget, and one busy user cannot lock out others behind the same IP. A request without a valid access token (missing, expired or forged) has no user, so it counts against its IP's partition; this also covers anonymous callers of per-user policies on `[AllowAnonymous]` endpoints such as `SearchUsers`. Because the limiter runs before authorization, an over-limit request is rejected with 429 even on an endpoint that would have answered 401. Ticket 07 covers a related gap: no forwarded-headers handling is configured, so behind a reverse proxy the "IP" is the proxy's address.

Per policy: auth endpoints are per-IP (the caller isn't authenticated yet): `Login` (10/hour), `Register` (5/hour), `Refresh` (5/10min), `RequestPasswordReset` (3/15min), `ResetPassword` (5/15min). Account-management endpoints (deactivate/reactivate/delete) are per-user (the anonymous activation endpoints are per-IP). A global sliding-window limiter (300/min, per user) backstops everything. See `RateLimitConfig.cs` for exact figures per policy.

Every rejection, from any endpoint policy or the global limiter, returns 429 with the `ApplicationResponse` envelope (`succeeded: false`, `message: ResponseMessages.TooManyRequests`), written by the limiter's `OnRejected` callback. Limiter state lives in the host, and every integration test builds its own `CustomWebApplicationFactory`, so a test that exhausts a policy (see `RateLimiting/RateLimitRejectionTests` and `RateLimiting/RateLimitPartitionTests`, which checks the per-user and per-IP partitions) can't rate-limit any other test.
