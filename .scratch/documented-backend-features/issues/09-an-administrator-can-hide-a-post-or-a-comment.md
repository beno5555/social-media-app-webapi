# 09: An administrator can hide a post or a comment

**What to build:** An administrator hides a post or a comment without deleting it, and it disappears for other users. Today the only way to take content away is to delete it.

Posts and comments gain a moderation status: `VISIBLE`, `FLAGGED` or `HIDDEN`. An administrator sets it with `PUT /api/admin/posts/{id}/moderation` and `PUT /api/admin/comments/{id}/moderation`, which accept any of the three. (The n8n spec will later let its service identity call the same two endpoints.)

The visibility rule applies to every read:

- **Hidden** content is returned only to its author and to administrators. For every other caller, including a guest, it is left out of every list (the feed, a user's posts, the comments of a post, the comments inside a full post) and reads as not found by id. Counts leave it out. Nobody who cannot read it can like it, comment on it or list its likes or comments.
- **Flagged** content is treated as visible.
- The author still sees their own hidden content, can delete it, and can edit it without changing its status.

Post and comment shapes gain a hidden flag, which only an author or administrator can ever see as true. The full status is not put in ordinary shapes, so a flag is never disclosed to ordinary users.

This ticket comes after photos, likes and the feed on purpose: those tickets rewrite the same reads, and the rule is applied in one sweep once they are settled. Check every read of a post or comment, not only the ones named here.

Decisions are under "Reports and moderation" in `.scratch/documented-backend-features/spec.md`. The moderation queue is ticket 10 and reports are tickets 11 and 12.

**Blocked by:** 05 (A post can be liked), 08 (The feed ranks friends' and other users' posts)

**Status:** done

**Difficulty:** tricky: a visibility rule across every read with three kinds of caller, where one missed read leaks hidden content.

- [x] Existing posts and comments are `VISIBLE` after the migration, and new ones start `VISIBLE`
- [x] An administrator sets a post's and a comment's status to any of the three values; an unknown id is not found
- [x] An ordinary user and a guest are refused on both moderation endpoints
- [x] A hidden post is absent from the feed and from its author's post list for another user and for a guest, and is not found by id
- [x] Another user cannot like or comment on a hidden post, and its likes and comments read as not found
- [x] A hidden comment is absent from the post's comment list and from the comments inside the full post for another user, and is not found by id
- [x] A post's comment count leaves out hidden comments
- [x] The author still gets their own hidden post or comment, in lists and by id, with the hidden flag true
- [x] The author can delete their hidden content, and editing it leaves it hidden
- [x] An administrator still gets hidden content, with the hidden flag true
- [x] Flagged content is returned to everyone with the hidden flag false, and nothing in an ordinary shape reveals that it is flagged
- [x] Setting hidden content back to `VISIBLE` returns it to every read
- [x] Each visibility case is tested from three callers: another user, the author and an administrator
- [x] A status change writes a `Log` row, and each endpoint has its own rate-limit policy
- [x] `docs/backend/controllers.md`, `docs/backend/data.md` and `README.md` describe moderation status and the visibility rule
