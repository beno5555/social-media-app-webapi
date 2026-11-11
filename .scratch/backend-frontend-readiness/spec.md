# Backend frontend-readiness

Outcome of the backend assessment grill on 2026-11-11. Goal: remove everything in the backend that the Angular frontend would trip over, before the monorepo move. Deadline for the full-stack app: 2026-11-25.

## Decisions

- **Scope.** Fix bugs, frontend blockers and docs drift now. Everything else is deferred (see below).
- **Deactivation.** Deactivating another user's account is administrator-only. (Fixed in the working tree, uncommitted, together with the swapped rate-limit policies and the `CreatePost` failure body.)
- **Single origin.** Frontend and backend share one origin in dev (Angular proxy for `/api` and `/hubs`) and in production (nginx). The refresh cookie stays `Secure` + `SameSite=Strict`. See `docs/adr/0001-single-origin-for-frontend-and-backend.md`.
- **Error shape.** Every non-validation failure returns `ApplicationResponse`. The frontend handles two shapes: `ApplicationResponse` and the automatic validation errors.
- **Pagination.** Infinite scroll everywhere; a page shorter than `pageSize` means the end. No total count from the backend.
- **Redirect allowlist.** `UrlSecurityService` reads `Cors:AllowedOrigins` instead of a hardcoded list. The unused `FrontendUrl` setting is removed.
- **API client.** Hand-written Angular services and DTO interfaces, with generic `ApplicationResponse<T>` and `ListResponse<T>` mirroring the C# classes. No OpenAPI generation.
- **Docs.** Docs follow the code where they disagree on HTTP verbs. Docs are retitled to WebCity. The chunked docs move to `docs/backend/` during the monorepo move, not before.
- **Layout.** `backend/` + `frontend/` + `docs/` at the repo root.
- **Hosting.** Not required for the deadline; deferred. If time is left: one DigitalOcean Droplet, Docker Compose, nginx, DuckDNS subdomain.
- **Database.** SQL Server through the deadline (SQL Server Express in a container if deployed).

## Guardrails that keep the app deployable

1. The frontend only calls relative URLs (`/api/...`, `/hubs/...`).
2. No environment value is hardcoded; origins and URLs come from config.
3. The refresh cookie is never loosened for local convenience.

## Deferred until after the deadline

- `POST` instead of `PATCH`/`PUT` for logout, request-password-reset and reset-password.
- PostgreSQL switch. Watch for: raw T-SQL in `GetConversationFriendsAsync`, the test fixture and Respawn setup, and case-sensitive text comparison affecting usernames, emails and search.
- Deployment (see Hosting above).
- Pagination totals.
- Links (instead of raw tokens) in the reactivation and failed-register emails.

## Order of work

Commit the uncommitted fixes first, then tickets 01 to 04 in order. Ticket 01 comes first because it makes the integration suite safe to run as the baseline for the others.
