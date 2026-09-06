# AspNetProject — Authentication & Authorization

> Part of the AspNetProject doc set. See also: `core.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

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
- Rotated on every refresh: old token revoked, new one issued via the shared `AccountSecurityService.IssueAccessAndRefreshTokensAsync` helper. Revoked (not deleted) on logout; a background job (`RefreshTokenCleanupService`) later purges expired/revoked rows — see `background-jobs.md`.

### Auth flow

1. **Register** (`POST api/auth/register`) — creates the user (no roles beyond none assigned at registration; roles are assigned separately by an admin). Fails on duplicate email or username. On duplicate email, triggers `AccountSecurityService.HandleFailedRegisterAttempt` — emails the existing account holder a password-reset token, in case the registration attempt was the real owner locked out of their account.
2. **Login** (`POST api/auth/login`) — validates credentials, issues access token (body) + refresh token (cookie) via `IssueTokensAndRefreshAsync`-style helper (`AccountSecurityService.IssueAccessAndRefreshTokensAsync`).
3. **Authenticated requests** — client attaches `Authorization: Bearer {accessToken}`.
4. **Refresh** (`POST api/auth/refresh`) — no body; refresh token cookie read automatically, hashed, looked up, revoked, new pair issued.
5. **Logout** (`POST api/auth/logout`) — revokes the current refresh token (if any) and clears the cookie.
6. **Password reset** — `POST api/auth/request-password-reset` issues a reset token (reusing `User.ResetTokenHash`/`ResetTokenExpiresAt`) and emails it; `POST api/auth/reset-password` consumes it, rejects if the new password equals the current one, revokes all the user's refresh tokens (forces re-login everywhere), and emails a "your password was reset" notice.

### Account activation / deactivation (separate from auth, own controller/service)

- **Deactivation** (`AccountActivationController`/`AccountActivationService`) — sets `AccountDeactivatedAt`, revokes all refresh tokens immediately, emails the user. Self-service (`PATCH api/accounts/mine/deactivate`) and admin-on-other-user (`PATCH api/accounts/{id}/deactivate`) are separate endpoints/rate-limit policies but call the same service method.
- **Reactivation** — two paths: admin-initiated (`PATCH api/accounts/{id}/activate`, `Administrator`-only, immediate) and self-service via emailed token (`PATCH api/accounts/request-activate` to request the token, `PATCH api/accounts/activate` with the token in the body to confirm — both `[AllowAnonymous]`, since a deactivated user can't authenticate).
- Both activation-token issuance and password-reset-token issuance share `User.ResetTokenHash`/`ResetTokenExpiresAt` — different service methods (`HandleAccountActivationRequest` vs `HandlePasswordResetRequest`), same underlying fields, cleared on successful use.

### Roles

- Seeded via `RoleConfiguration.HasData`: `User` (Id 1), `Administrator` (Id 2). A user row is also seeded with both roles (`UserRoleConfiguration`, `UserId = 3114`) for local dev/testing.
- `AccountManagementController` exposes `Administrator`-only assign/unassign endpoints (`PUT api/accounts/{id}/assign-administrator`, `PUT api/accounts/{id}/remove-administrator`).
- `BaseController.IsAdministrator()` checks `User.IsInRole(nameof(RoleName.Administrator))`; used by `PostController`/`CommentController` to let an admin delete any post/comment, and by `AccountManagementController`/`AccountActivationController` for admin-vs-self routes.
- No self-registration path grants roles — a new user has none until an admin assigns one.

### Error handling / response conventions

- Auth failure messages are deliberately generic and uniform: login doesn't distinguish "no such user" from "wrong password."
- `LogoutAsync` returns `void` — no failure state by design.
- `RegisterAsync` returns `ApplicationResponse<FullUserDto>` with the created user, matching REST convention of returning the created resource. Registration does not implicitly log the user in.
- Every `AuthService`/`AccountActivationService`/`AccountManagementService` method logs its outcome via `BaseService.LogResultAsync` (see `core.md`), in addition to returning an `ApplicationResponse`.

### Global exception handling

- `GlobalExceptionHandler : IExceptionHandler` (registered via `AddExceptionHandler<T>()` + `AddProblemDetails()`) logs the exception through `SystemLogger.LogEndpointErrorAsync`, then maps `DbUpdateException` → `409 Conflict` (everything else → `500`), with full exception detail returned only in `Development`.
- `SystemLogger` writes to a rolling text file (`Logs/ErrorLogs.txt`) under a `SemaphoreSlim`-guarded async write — separate from the structured `DatabaseLogger` (DB-backed, per-action) used everywhere else.

### Rate limiting

Auth endpoints are limited per-IP (the caller isn't authenticated yet): `Login` (10/hour), `Register` (5/hour), `Refresh` (5/10min), `RequestPasswordReset` (3/15min), `ResetPassword` (5/15min). Account-management endpoints (deactivate/reactivate/delete) are limited per-user (or per-IP for the anonymous activation-confirmation endpoints). A global per-user sliding-window limiter (300/min) backstops everything. See `RateLimitConfig.cs` for exact figures per policy.
