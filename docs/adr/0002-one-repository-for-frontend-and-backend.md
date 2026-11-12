# Frontend and backend live in one repository

WebCity's backend was built alone in this repository, and an Angular frontend is now being added. We keep both in this one repository, as `backend/` and `frontend/` folders with shared `docs/`, and we preserve the backend-only state as the git tag `api-only` instead of freezing it in a repository of its own. The two halves are always served from one origin (ADR 0001), are built by one developer, and share the same agent instructions, skills, glossary and issue tracker, so splitting them would duplicate all of that for no gain.

## Considered Options

- **A new repository for the monorepo, with this one left frozen as the API-only version**: rejected. This is what was done when the console application became a web API, but that was a new project type with no shared files. Here the same files continue in new folders. A second repository would mean carrying history across or losing it, and two places for CI, issues and settings. Its one advantage is a second entry on the owner's GitHub profile.
- **A separate repository for the frontend only**: rejected, because a change that touches an endpoint and its caller would span two repositories and two pull requests.

## Consequences

- The API-only version is the tree at tag `api-only`. It is a record of the backend before the frontend existed, including the rate-limit defect its own docs describe; it is not maintained.
- The choice stays open in one direction: a standalone API repository can still be created from the tag later, with full history.
- The tag must never be moved or deleted.
