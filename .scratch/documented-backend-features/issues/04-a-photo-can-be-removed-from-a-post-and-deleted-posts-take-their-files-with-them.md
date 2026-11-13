# 04: A photo can be removed from a post, and deleted posts take their files with them

**What to build:** A post's author (or an administrator) removes a single photo from the post with `DELETE /api/posts/{postId}/images/{imageId}`, and a post never ends up empty. When a post goes away, by its author, by an administrator or by the purge of a soft-deleted user, its photo files go with it. After ticket 02 the files of a deleted post stay on disk forever.

A post must keep a title and either text or at least one photo. So removing the last photo of a post that has no text is refused, and editing a post's text to empty is refused unless the post has photos. Editing itself stays a JSON, text-only request.

File removal happens after the database change and is best-effort: a file that cannot be removed is logged and does not fail the request.

The purge of soft-deleted users runs as a background job, which the integration test host removes, so that path is not covered by a test. Verify it by reading the code and by one manual run, and say so in a comment on this ticket.

Decisions are under "Images" in `.scratch/documented-backend-features/spec.md`.

**Blocked by:** 02 (A post can be created with photos)

**Status:** ready-for-agent

**Difficulty:** tricky: it changes the purge of soft-deleted users, a bulk-delete path that no test covers.

- [ ] The author removes a photo from their post; the post no longer lists it and its address no longer serves a file
- [ ] An administrator can remove a photo from any post; any other user is refused
- [ ] An unknown post, an unknown photo, and a photo that belongs to a different post are each not found
- [ ] Removing the last photo of a post with no text is refused, and the photo stays
- [ ] Editing a post's text to empty is refused when the post has no photos and accepted when it has
- [ ] The remaining photos keep their order
- [ ] Deleting a post removes its photo rows and its files
- [ ] The purge of soft-deleted users removes the files of the posts it deletes, verified by a manual run recorded on this ticket
- [ ] A file that cannot be removed is logged and the request still succeeds
- [ ] The new action has its own rate-limit policy
- [ ] `docs/backend/controllers.md`, `docs/backend/data.md` and `docs/backend/background-jobs.md` describe photo removal and file cleanup
