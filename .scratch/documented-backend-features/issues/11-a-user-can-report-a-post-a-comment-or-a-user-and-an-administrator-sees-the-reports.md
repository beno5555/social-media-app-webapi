# 11: A user can report a post, a comment or a user, and an administrator sees the reports

**What to build:** A signed-in user reports a post, a comment or another user, giving a reason, and an administrator sees the report in a list with the reported content beside it. Nothing can be reported today.

- **`POST /api/reports`** takes a target type (post, comment, user), a target id and a reason of bounded length. The target must exist and be readable by the caller, so content hidden from the caller is not found. Reporting oneself or one's own content is refused. A second report of the same target by the same user is refused while the first is still pending, and allowed again once it is decided.
- **`GET /api/reports`** is administrator-only, paged, newest first, filtered by status (pending when no status is given) and by target type. Each row carries the reporter, the reason, the status, the reviewer and the times, and a snapshot of the target: a post's title, text, photos and author; a comment's text, author and post; a user's username and bio. A target that no longer exists is shown as missing. The post and comment snapshots are the same shape as in the moderation queue (ticket 10); whichever ticket lands first creates it.

The report entity follows the documentation's model. A report points at a user twice (reporter and reviewer), and SQL Server refuses two cascading paths to one table, so the database cannot clean up both. When the purge of soft-deleted users removes a user, the reports they made are deleted and their reviewer reference on other reports is cleared, explicitly where the database cannot do it. The integration suite applies migrations to a real SQL Server, so a wrong delete rule fails loudly.

Reports are not cleaned up when their target is deleted. Deciding a report is ticket 12.

Decisions are under "Reports and moderation" in `.scratch/documented-backend-features/spec.md`.

**Blocked by:** 09 (An administrator can hide a post or a comment)

**Status:** done

**Difficulty:** routine: a new entity with validation and a filtered list on existing patterns; the one trap (delete rules) is spelled out and caught by the suite's migration run.

- [x] A user reports a post, a comment and another user, each with a reason, and the report is created as pending
- [x] A report with no reason, or a reason over the length limit, is refused
- [x] A report of an unknown target, or of content hidden from the caller, is not found
- [x] A report of oneself, of one's own post or of one's own comment is refused
- [x] A second pending report of the same target by the same user is refused; a different user can still report it
- [x] A guest cannot report
- [x] An administrator gets reports page by page, newest first, pending only unless a status is given
- [x] The list can be narrowed by status and by target type
- [x] Each row shows the reporter, the reason, the status and a snapshot of the target, and a deleted target is shown as missing
- [x] An ordinary user and a guest are refused on the list
- [x] The purge of soft-deleted users deletes the reports those users made and clears their reviewer reference
- [x] Each new action has its own rate-limit policy
- [x] `docs/backend/controllers.md`, `docs/backend/data.md`, `docs/backend/background-jobs.md` and `README.md` describe reports
