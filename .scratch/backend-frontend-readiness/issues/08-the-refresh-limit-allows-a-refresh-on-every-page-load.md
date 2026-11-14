# 08: The refresh limit allows a refresh on every page load

**What to build:** A signed-in user can reload the page as often as a person normally does and stay signed in. The frontend keeps the access token only in memory (ADR 0004), so every page load, in every tab, calls the refresh endpoint to restore the session. Today that endpoint allows 5 calls per 10 minutes per IP: the sixth reload within ten minutes is rejected with 429, and the user appears signed out although their refresh token is still valid. During frontend development, where the dev server reloads the page on code changes, the limit is reached within minutes. Raise the limit to 60 per 10 minutes per IP. The endpoint stays rate-limited and stays per-IP, and no other policy changes.

Out of scope: until ticket 07 lands, everyone behind the proxy shares this one budget, like every other per-IP limit. That is ticket 07's problem and is not solved here.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Difficulty:** routine: one number in the existing rate-limit configuration, with an existing rejection test to adapt.

- [ ] A test, written failing first, calls the refresh endpoint six times in a row from one address, and the sixth call is not rejected with 429
- [ ] The existing test that exhausts the refresh policy still reaches a 429 with the failure envelope
- [ ] The auth doc states the new limit
- [ ] The full integration suite passes

## Comments

- From ticket 06's review: `RateLimitRejectionTests` stops trying after `MaxAttempts = 50`, so once Refresh allows 60 per 10 minutes the exhaust loop never reaches a 429 and the existing rejection test fails. Raise the cap above the new limit while adapting that test. The same exhaust loop (send until the status is no longer the "still allowed" one, capped by `MaxAttempts`) is copied in `RateLimitPartitionTests.ExhaustSearchUsersAsync`. Since this ticket has to change the loop anyway, consider moving it into one shared helper in `Helpers/` that takes the request and the "still allowed" status, used by both classes.
