# 12: An administrator decides a report

**What to build:** An administrator closes a pending report with one of two decisions, through `PUT /api/reports/{id}`:

- **Hide.** A reported post or comment becomes `HIDDEN`, so other users stop seeing it (ticket 09's rule). A reported user's account is deactivated through the existing administrator deactivation path, with everything that path already does. The report, and every other pending report on the same target, becomes `RESOLVED` with the reviewer and the time. All of it happens in one transaction, so the administrator decides once per target.
- **Reject.** This report becomes `REJECTED` with the reviewer and the time. The target and every other report are left alone.

A report that is not pending refuses a second decision. A hide on a target that no longer exists is refused and the report stays pending; such a report can still be rejected. If two administrators decide the same report at the same moment, one succeeds and the other is refused, and the effect happens once.

The moderation endpoints from ticket 09 do not touch reports, and rejecting a report does not change a target's moderation status. An administrator who wants hidden content back sets its status directly.

Decisions are under "Reports and moderation" in `.scratch/documented-backend-features/spec.md`.

**Blocked by:** 11 (A user can report a post, a comment or a user, and an administrator sees the reports)

**Status:** ready-for-agent

**Difficulty:** tricky: one transaction across reports and their target, reuse of the account deactivation path, and two administrators deciding at once.

- [ ] Hiding a reported post or comment makes it hidden for other users and resolves the report with the reviewer and the time
- [ ] Hiding a reported user deactivates the account, with the same effects as an administrator deactivating it directly
- [ ] Hiding resolves every other pending report on the same target in the same transaction
- [ ] Rejecting sets the report to rejected with the reviewer and the time, and leaves the target and other reports unchanged
- [ ] A report that is already resolved or rejected refuses a decision
- [ ] A hide on a target that no longer exists is refused and the report stays pending; rejecting it works
- [ ] An unknown report is not found
- [ ] An ordinary user and a guest are refused
- [ ] A second decision on the same report made concurrently is refused, and the hide takes effect once
- [ ] Each decision writes a `Log` row, and the endpoint has its own rate-limit policy
- [ ] `docs/backend/controllers.md` and `README.md` describe report decisions
