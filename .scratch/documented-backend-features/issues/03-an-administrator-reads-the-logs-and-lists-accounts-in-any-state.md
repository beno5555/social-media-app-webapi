# 03: An administrator reads the logs and lists accounts in any state

**What to build:** An administrator can read the log rows WebCity already writes, and can list accounts whatever state they are in. Neither is possible today: the log service is an empty class that nothing calls, and every user read hides deactivated and soft-deleted accounts, so an administrator cannot find a deactivated account in order to restore it.

Both reads live in a new administrator-only controller at `api/admin`, which later tickets in this spec extend.

- **`GET /api/admin/logs`:** paged, newest first, filtered by user, authorized or not, succeeded or not, entity name, entity id, action, and a date range. The log service takes this job and reads through the log repository, not the database context.
- **`GET /api/admin/users`:** paged, filtered by account state (active, deactivated, soft-deleted) and by username. Each row shows who the account is, whether it is an administrator, and when it was created, deactivated or deleted. This endpoint is not in the documentation's list; it is added because the admin panel needs it.

A successful admin read writes no `Log` row, so reading the logs does not fill them.

Decisions are under "Admin reads" in `.scratch/documented-backend-features/spec.md`. Statistics is ticket 13.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Difficulty:** routine: two filtered, paged reads that follow the existing controller, service, repository and mapper patterns.

- [ ] An administrator gets log rows page by page, newest first
- [ ] Each log filter narrows the result: user, authorized, succeeded, entity name, entity id, action, date range; filters combine
- [ ] The log service reads through the log repository
- [ ] An administrator gets accounts page by page, including deactivated and soft-deleted ones, each row showing its state
- [ ] The account list can be narrowed by state and by username
- [ ] An ordinary user is refused on both endpoints, and so is a guest
- [ ] Each endpoint has its own rate-limit policy
- [ ] A successful read of either list writes no `Log` row
- [ ] `docs/backend/controllers.md` and `README.md` describe the admin controller and both reads
