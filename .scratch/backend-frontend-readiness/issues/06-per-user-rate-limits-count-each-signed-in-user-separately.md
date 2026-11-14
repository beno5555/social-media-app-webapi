# 06: Per-user rate limits count each signed-in user separately

**What to build:** A signed-in user's requests count against their own rate-limit budget, so one busy user on a shared network (a campus or office Wi-Fi behind one public IP) can no longer lock out everyone else on it. Today the rate limiter runs before authentication, so it never sees who is calling: every "per user" policy, and the global limiter, quietly counts by IP instead. Rate limiting should run after authentication has read the access token and before authorization. Anonymous endpoints (login, register, refresh, password reset, activation) stay per-IP on purpose.

Out of scope: behind a reverse proxy the backend sees the proxy's IP rather than the client's, which ticket 07 covers.

**Blocked by:** None (can start immediately)

**Deferred:** not scheduled until deployment work begins.

**Status:** done

**Difficulty:** tricky: middleware order is an auth boundary, and the test must exhaust one user's budget without the shared test IP or the global limiter masking the result.

- [x] Rate limiting runs after authentication and before authorization
- [x] An integration test, written failing first, exhausts a per-user policy as one user and then shows a second user from the same IP is still allowed
- [x] A request with a missing or expired access token still falls back to its IP partition
- [x] Anonymous policies remain per-IP; limits and windows are unchanged
- [x] Every doc that ticket 05 marked as "currently per-IP, see ticket 06" (auth doc, README, project instructions) now describes per-user partitioning, with the pointer to this ticket removed
- [x] The full integration suite passes when run twice in a row
