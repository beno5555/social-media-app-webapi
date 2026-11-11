# 05: Docs match the code

**What to build:** A coding agent reading WebCity's docs gets true statements. Every known disagreement between the docs and the code is corrected in the docs, the two unaudited docs are checked, and every doc calls the product WebCity. The docs stay in their current folder; moving them belongs to the monorepo restructure.

Known disagreements (the doc is wrong in each):

| Doc | Doc says | Code does |
|---|---|---|
| Controllers doc, account activation section | Deactivating another user's account is open to any signed-in user, and the rate-limit policies look swapped | Administrator-only; policies are correct |
| Auth doc, auth flow | Logout is `POST` | `PATCH` |
| Auth doc, auth flow | Both password-reset endpoints are `POST` | Both are `PUT` |
| Auth doc | Nothing about the reset return address or how it is checked | Exists; final behaviour set by ticket 04 |
| README (twice) and SignalR doc | Cross-origin policy is `SignalRTestPolicy` | `OriginPolicy` |
| Project instructions and auth doc | The exception handler's output | Final shape set by ticket 02 |
| Auth doc | Points to a `core.md` | No such doc |
| All five per-area docs | Titled "AspNetProject" | The product is WebCity (see the glossary) |
| Auth doc, README (twice) and project instructions | Limits are per-IP before login and per-user after, and the global limiter is per-user | Every policy, including the global limiter, counts by IP, because rate limiting runs before authentication; ticket 06 fixes this |

**Blocked by:** 02 (Refresh and server errors return the failure envelope), 03 (Rate-limit rejections return the failure envelope), 04 (Reset links accept configured origins)

**Status:** done

**Difficulty:** routine: the corrections are listed, and the rest is checking stated facts against the code.

- [x] Every row in the table is corrected
- [x] The docs describe the two failure shapes a client can receive: the envelope (including for refresh, server errors and rate limits) and validation errors
- [x] The data doc and the background-jobs doc are checked against the code, statement by statement; anything wrong is fixed, and a comment on this ticket lists what was found
- [x] All docs name the product WebCity; the code project and namespace names are unchanged
- [x] The "known drift" note in the project instructions is removed once nothing it lists remains
- [x] The project instructions' description of the hardcoded redirect allowlist is replaced with the configured behaviour
- [x] Wherever the docs describe rate-limit partitioning, they say every limit currently counts by IP and point to ticket 06 as the fix
- [x] No doc file is moved or renamed

## Comments

**Audit of `data.md` and `background-jobs.md` against the code** (statement by statement; everything not listed below was correct):

`background-jobs.md`
- Wrong: "Derived services take only their own `IOptions<TConfig>` plus `IServiceScopeFactory`, forwarding both to `base(...)`". The base constructor takes `(IServiceScopeFactory, TimeSpan interval)`; derived classes forward the scope factory and `config.Value.Interval` and keep `config.Value` in a field. Fixed.
- Missing, and a code gap rather than a doc error: the base class logs a failed cycle with `entityName: null`, but `Log.EntityName` is a required column, so that insert fails and `DatabaseLogger` falls back to `SystemLogger` (`Logs/ErrorLogs.txt`). Documented as a known gap. The code is unchanged. Worth a ticket.
- Verified correct: all config defaults, the expired-and-revoked purge condition, the messages, friendships, posts, user purge order, the `SoftDeleteCleanupResult` accumulation, `LogSystemActionAsync` behaviour (`AuthorizedRequest = false`, `UserId = null`).

`data.md`
- Wrong: `ClearTracker()` "used before soft-delete's related-data cleanup sequence". Its only caller is the start of the soft-delete transaction, and soft-delete removes no related data (the purge job does). Fixed, along with the same claim in the README.
- Imprecise: the `Log` row now states `Details` (max 1000) and `EntityName` (required, max 50).
- Verified correct: repository method list and visibility, pagination defaults (10 / max 100), entity fields, `BaseEntity`, the check constraint, the query filter, all FK delete behaviours.

Other facts found wrong while checking:
- `auth.md`: the seeded admin was said to be `UserId = 3114`; it is Id 1 (`sandro_beno`). Also `SystemLogger` was called a "rolling" file; it appends to a single file.
- `CLAUDE.md` and the README said `SystemLogger` records background-job failures; background jobs log through `DatabaseLogger` (see the gap above).

Things noticed but not changed (not docs):
- `DeterministicFixtureSeed` (alice, bob and others) is applied with `HasData`, so those fixture users are part of the migrations and will exist in any database the migrations run against, including production. Documented in `auth.md`; worth deciding whether that is intended.
- The README mentioned `SignalRTestPolicy` once, not twice as the ticket table says.
- Framework-level `401`/`403`/unmatched-route `404` responses have no body, so they are a third thing a client sees. The docs say so.
- Docs-only change: the integration suite was not run.
