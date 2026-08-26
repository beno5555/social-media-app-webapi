# AspNetProject — Authentication & Authorization

> Part of the AspNetProject doc set. See also: `core.md`, `data.md`, `auth.md`, `controllers.md`.

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