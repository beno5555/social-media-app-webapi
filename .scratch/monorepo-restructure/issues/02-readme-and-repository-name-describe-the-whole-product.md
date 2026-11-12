# 02: README and repository name describe the whole product

**What to build:** A visitor to the repository reads about WebCity as it now is: a full-stack product with a backend and a frontend. Today's `README.md` describes the backend alone and spends much of its length on the console application the API grew out of. That history belongs to the API-only version, which stays readable at tag `api-only` (ADR 0002), and should leave the current README. The GitHub repository name `social-media-app-webapi` is wrong for the same reason and is renamed in the same piece of work.

**Blocked by:** 01 (Backend and docs move into the monorepo layout), and the frontend existing: what a product README should say depends on what the frontend turns out to be.

**Status:** ready-for-agent

**Difficulty:** routine: writing, checked against the code.

## Open decisions (ask the owner; do not assume)

- **One README or three.** Either a single root README covering both halves, or a short root README plus `backend/README.md` and `frontend/README.md`. The owner left this open on 2026-11-12.
- **The new repository name.** `webcity` was suggested as an example, not agreed.

## Notes

- The owner renames the repository in GitHub's settings; an agent cannot. GitHub redirects the old URL. The local root folder is not renamed. A cloud environment tied to the old name may need the repository selected again.
- Design decisions in the current README that are still true and useful move to `docs/backend/` or stay; they are not simply deleted.

## Acceptance

- [ ] The README describes WebCity as a product, names both halves, and tells a reader how to run each
- [ ] The console-application history is gone from the README, with one line pointing to tag `api-only` for the earlier state
- [ ] Every statement in the README is true of the code
- [ ] The repository is renamed, and `CLAUDE.md` and any doc that names the old repository are updated
