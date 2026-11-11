# 03 - Redirect allowlist from config

Status: ready-for-agent
Difficulty: routine
Blocked by: 01

## Problem

`Infrastructure/Services/Helpers/UrlSecurityService.cs` checks password-reset redirect URLs against a hardcoded set containing only `http://localhost:5500`. The Angular frontend will run on a different origin, and hardcoded origins break guardrail 2 in the spec.

## What to do

- Have the redirect check use the origins in `Cors:AllowedOrigins` instead of the hardcoded set.
- `UrlSecurityService` is currently a static class; it needs configuration, so make it a registered concrete class (no interface) in `Extensions/ApplicationServicesExtensions.cs` and inject it where it is used.
- Keep the comparison on scheme + authority, case-insensitive, as today.
- Update or add integration tests: an allowed origin produces a link, a disallowed one is rejected.
- Update `CLAUDE.md` ("Frontend integration points") and `auth.md`.

## Done when

- No origin is hardcoded in `UrlSecurityService`.
- The integration suite passes.
