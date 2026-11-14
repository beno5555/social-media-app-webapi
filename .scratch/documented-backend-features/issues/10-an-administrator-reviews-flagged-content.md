# 10: An administrator reviews flagged content

**What to build:** An administrator opens a queue of the posts and comments that are marked `FLAGGED` and sees enough of each to decide, then approves or hides it with the moderation endpoints from ticket 09.

`GET /api/admin/moderation-queue/posts` and `GET /api/admin/moderation-queue/comments` are administrator-only and paged. Each returns the flagged content of one type, oldest first. One route per type avoids paging across two tables. (Changed by the owner on 2026-11-15: this ticket first asked for one route with a query parameter. A comment's snapshot also carries its post's title.)

Each row carries a snapshot of the content:

- for a post: its id, title, text, photos, author and creation time;
- for a comment: its id, text, author, the post it belongs to and creation time.

The report list in ticket 11 shows the same snapshot for a reported post or comment. Whichever of the two tickets lands first creates the shape, and the other reuses it.

Until the n8n spec is built, content becomes flagged only when an administrator sets that status, which is how the tests fill the queue.

Decisions are under "Reports and moderation" in `.scratch/documented-backend-features/spec.md`.

**Blocked by:** 09 (An administrator can hide a post or a comment)

**Status:** done

**Difficulty:** routine: one paged read per content type that follows existing patterns, built on the status from ticket 09.

- [x] An administrator gets the flagged posts, page by page, oldest first, each with its snapshot
- [x] An administrator gets the flagged comments the same way, chosen by the query parameter
- [x] Visible and hidden content never appear in the queue
- [x] Content leaves the queue once an administrator sets it to `VISIBLE` or `HIDDEN`
- [x] An ordinary user and a guest are refused
- [x] The endpoint has its own rate-limit policy
- [x] `docs/backend/controllers.md` and `README.md` describe the moderation queue
