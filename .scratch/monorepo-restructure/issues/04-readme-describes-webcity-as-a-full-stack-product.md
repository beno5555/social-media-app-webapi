# 04: README describes WebCity as a full-stack product

**What to build:** A visitor to the repository reads about WebCity as it is once the frontend exists: a full-stack product with a backend and an Angular frontend, served from one origin (ADR 0001). The README names both halves and tells a reader how to run each. What it says about the frontend depends on what the frontend turns out to be, which is why this ticket waits for the scaffold.

Together with 03, this replaces the former ticket 02.

**Blocked by:** 03 (README describes the backend as it is now, and the repository is called webcity), and the Angular frontend being scaffolded in `frontend/` (no ticket exists for that yet).

**Status:** ready-for-agent

**Difficulty:** routine: writing checked against the code, once the owner has settled the open decision below.

## Open decisions (ask the owner; do not assume)

- **One README or three.** Either a single root README covering both halves, or a short root README plus `backend/README.md` and `frontend/README.md`. The owner left this open on 2026-11-12.

## Acceptance

- [ ] The README describes WebCity as a product, names both halves, and tells a reader how to run each
- [ ] Every statement in the README (or READMEs) is true of the code
- [ ] `CLAUDE.md`'s description of the README matches the chosen layout
