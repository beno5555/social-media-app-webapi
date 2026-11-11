# 04 - Fix docs drift

Status: ready-for-agent
Difficulty: routine
Blocked by: 02, 03

## Problem

Agents rely on the docs, and several statements no longer match the code.

## Known drift (docs follow the code)

| Where | Doc says | Code does |
|---|---|---|
| `controllers.md`, `AccountActivationController` section | `{id}/deactivate` is reachable by any authenticated user; rate-limit policies "look swapped" | Administrator-only; policies correct |
| `auth.md`, auth flow | Logout is `POST` | `PATCH api/auth/logout` |
| `auth.md`, auth flow | Both password-reset endpoints are `POST` | Both are `PUT` |
| `auth.md` | No mention of the reset `RedirectUrl` or its allowlist | Added in commit `d13e601`; allowlist changes in ticket 03 |
| `README.md` (DI setup, twice) and `signalr.md` (CORS) | Policy `SignalRTestPolicy` | `OriginPolicy` |
| `CLAUDE.md` | `GlobalExceptionHandler` writes ProblemDetails | See ticket 02 for the final shape |
| `auth.md` | "see `core.md`" | No such file |
| All five chunked docs | Titled "AspNetProject" | Product name is WebCity (see `GLOSSARY.md`) |

## What to do

- Fix every row above.
- Verify `data.md` and `background-jobs.md` against the code; they were not checked line by line during the assessment.
- Remove the "Known drift" sentence from `CLAUDE.md` once the CORS name is fixed.
- Do not move the files: the move to `docs/backend/` happens in the monorepo restructure.

## Done when

- Each row is corrected, and any further drift found in `data.md` and `background-jobs.md` is fixed or listed in a comment below.
