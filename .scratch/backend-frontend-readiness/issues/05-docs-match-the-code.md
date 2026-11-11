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

**Blocked by:** 02 (Refresh and server errors return the failure envelope), 03 (Rate-limit rejections return the failure envelope), 04 (Reset links accept configured origins)

**Status:** ready-for-agent

**Difficulty:** routine: the corrections are listed, and the rest is checking stated facts against the code.

- [ ] Every row in the table is corrected
- [ ] The docs describe the two failure shapes a client can receive: the envelope (including for refresh, server errors and rate limits) and validation errors
- [ ] The data doc and the background-jobs doc are checked against the code, statement by statement; anything wrong is fixed, and a comment on this ticket lists what was found
- [ ] All docs name the product WebCity; the code project and namespace names are unchanged
- [ ] The "known drift" note in the project instructions is removed once nothing it lists remains
- [ ] The project instructions' description of the hardcoded redirect allowlist is replaced with the configured behaviour
- [ ] No doc file is moved or renamed
