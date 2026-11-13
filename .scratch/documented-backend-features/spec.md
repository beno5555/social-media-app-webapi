# Documented backend features

Status: ready-for-agent

## Problem Statement

The documentation sent to the principal (`webcity-documentation.md`) is the commitment for 2026-11-25. It describes things WebCity's backend does not do yet, and the Angular pages that are about to be built need them:

- A post or a message can only be text. No photo can be uploaded anywhere.
- A post cannot be liked.
- The feed shows only friends' posts, newest first. A new user with no friends sees nothing, and nobody discovers anyone.
- A user cannot see that a friend is typing.
- An administrator cannot read the logs WebCity writes, has no statistics, and cannot find a deactivated account in order to restore it.
- A user cannot report anything, and an administrator cannot hide content short of deleting it.

## Solution

Build the six missing areas, in the shape the documentation gives them, without changing what already works:

- **Images.** A post carries up to 10 photos and a message up to 5. Photos are checked, stored on the server's disk, and only their address is stored in the database.
- **Likes.** A user likes and unlikes a post, the author is told at once, and every post shows its like count and whether the caller liked it.
- **Feed ranking.** The feed shows friends' and other users' posts, ordered by a score built from the caller's connection to the author, the post's activity and its age.
- **Typing.** A user's client tells the hub they are typing, and the friend they are writing to sees it.
- **Admin reads.** An administrator reads the logs with filters, reads system statistics, and lists accounts in any state.
- **Reports and moderation.** A user reports a post, a comment or another user. An administrator reviews reports, hides content or rejects the report, and reviews content that has been flagged. Hidden content disappears for other users.

n8n and everything it drives (webhooks, the API key, the internal endpoints, AI moderation, email through n8n) is a separate spec.

## User Stories

### Images

1. As a user, I want to attach several photos when I create a post, so that I can share more than text.
2. As a user, I want to publish a post that has photos and no text, so that a picture can stand on its own.
3. As a user, I want to publish a post that has text and no photos, so that text posts keep working as they do today.
4. As a user, I want my photos to appear in the order I attached them, so that the post reads the way I meant it.
5. As a user, I want a clear message when a file is not an accepted image format, so that I know to pick a different file.
6. As a user, I want a clear message when a photo is too large, so that I know to shrink it.
7. As a user, I want a clear message when I attach too many photos, so that I know the limit.
8. As a user, I want a post with one bad file to be refused as a whole, so that I never publish a post that is missing a photo I chose.
9. As a post author, I want to remove one photo from my post, so that I can fix a mistake without deleting the post.
10. As a post author, I want to be stopped from removing the last photo of a post that has no text, so that I do not leave an empty post behind.
11. As a reader, I want every post I read to carry the addresses of its photos, so that the page can show them.
12. As a guest, I want to see the photos of a post I am allowed to read, so that public posts look the same signed in or out.
13. As a user, I want to send a friend one or several photos in a message, with or without text, so that chat is not limited to text.
14. As a message receiver, I want a photo message to arrive live with its photos, so that I see it without reloading.
15. As a user, I want a conversation's history to include each message's photos, so that old photos are still there.
16. As a user, I want the conversation list to tell me when the last message was a photo with no text, so that the entry is not blank.
17. As a user, I want the photos of a message or post to be removed from the server when it is deleted, so that deleted content is really gone.
18. As a user, I want the photos of a conversation to be removed when the friendship is removed, so that they go with the rest of the history.
19. As an operator, I want uploaded files to be named by the server and checked by their content, so that nobody can plant a script or overwrite a file by choosing a file name.
20. As an operator, I want the image limits and the upload folder to come from configuration with safe defaults, so that an existing setup keeps running without a config change.

### Likes

21. As a user, I want to like a post, so that I can show I appreciate it.
22. As a user, I want to take my like back, so that a mistaken like is not permanent.
23. As a user, I want a second like on the same post to be refused, so that one person counts once.
24. As a post author, I want to be told at once when someone likes my post, so that I see reactions as they happen.
25. As a post author, I want no notification for liking my own post, so that I am not notified about myself.
26. As a reader, I want every post to show how many likes and comments it has, so that I can see how active it is.
27. As a signed-in reader, I want every post to say whether I liked it, so that the like button shows the right state.
28. As a reader, I want to see who liked a post, page by page, so that I can find people with similar taste.
29. As a guest, I want like counts on the posts I can read, so that the public view is complete.

### Feed ranking

30. As a user, I want my feed to include posts by people I am not friends with, so that I discover new people and popular content.
31. As a user, I want friends' posts to rank above other posts of the same age and activity, so that I do not miss my friends' news.
32. As a user, I want posts by people who share a friend with me to rank above posts by strangers, so that the people I am likely to know come first.
33. As a user, I want a post with more likes and comments to rank above a quieter post of the same age and connection, so that popular content surfaces.
34. As a user, I want a newer post to rank above an older one of the same connection and activity, so that the feed stays fresh.
35. As a new user with no friends, I want a feed that is not empty, so that the app is worth opening.
36. As a user, I want my own posts left out of my feed, so that the feed is about other people.
37. As a user, I want the feed to load page by page, so that it opens quickly.

### Typing

38. As a user in a conversation, I want to see when my friend is typing, so that I know a reply is coming.
39. As a user, I want my typing signal to reach only the friend I am writing to, so that nobody else learns what I am doing.
40. As a user, I want a typing signal from someone who is not my friend to never reach me, so that strangers cannot ping me.

### Admin reads

41. As an administrator, I want to read the logs page by page, newest first, so that I can follow what the system is doing.
42. As an administrator, I want to filter logs by user, success or failure, action, entity and date range, so that I can find the rows about one incident.
43. As an administrator, I want totals of accounts, posts, comments, messages and likes, so that I know the size of the platform.
44. As an administrator, I want to see how many accounts, posts, comments and messages were created in the last 7 and 30 days, so that I can see activity.
45. As an administrator, I want to see how many users are online now and how many reports are waiting, so that I know what needs attention.
46. As an administrator, I want to list accounts in any state and filter by state and username, so that I can find a deactivated account and restore it.
47. As a user who is not an administrator, I want every admin endpoint to refuse me, so that logs and statistics stay private.

### Reports and moderation

48. As a user, I want to report a post with a reason, so that an administrator looks at it.
49. As a user, I want to report a comment with a reason, so that an administrator looks at it.
50. As a user, I want to report another user with a reason, so that an administrator looks at the account.
51. As a user, I want to be stopped from reporting myself or my own content, so that reports mean something.
52. As a user, I want a second report of the same thing to be refused while my first is still waiting, so that I cannot flood the list.
53. As an administrator, I want a list of reports filtered by status and by what was reported, so that I can work through the waiting ones.
54. As an administrator, I want each report to show the reported content or account alongside the reason, so that I can decide without leaving the list.
55. As an administrator, I want to hide the reported post or comment, so that other users stop seeing it.
56. As an administrator, I want hiding a reported user to deactivate the account, so that one decision deals with the account.
57. As an administrator, I want hiding to close every waiting report on the same target, so that I decide once.
58. As an administrator, I want to reject a report and leave the content alone, so that a groundless report has no effect.
59. As an administrator, I want a report that is already decided to refuse a second decision, so that two administrators do not undo each other.
60. As an administrator, I want a queue of flagged posts and flagged comments, so that I can review what was marked as doubtful.
61. As an administrator, I want to set a post's or comment's moderation status directly, so that I can approve flagged content or restore something hidden by mistake.
62. As a user, I want a hidden post or comment by someone else to be gone from every list and to read as not found, so that hidden really means hidden.
63. As an author, I want to still see my own hidden post or comment, marked as hidden, so that I know what happened to it.
64. As an administrator, I want to still read hidden content, so that I can review it.
65. As a reader, I want flagged content to stay visible until an administrator decides, so that doubtful content is not removed on suspicion alone.
66. As a reader, I want comment counts and the comments inside a post to leave out hidden comments, so that the numbers match what I can see.

### For the people building on this

67. As a frontend developer, I want every new failure to use the existing failure envelope or the validation shape, so that the one error handler still covers everything.
68. As a frontend developer, I want image addresses to be relative and to start under `/api`, so that the dev proxy and the single origin need no new rule.
69. As a developer, I want every new action to have its own rate-limit policy, so that the house pattern has no holes.
70. As a developer, I want the purge of soft-deleted users to keep working with the new tables, so that the background job does not start failing in production.
71. As a coding agent, I want the per-area docs and the README updated with each ticket, so that the docs keep matching the code.

## Implementation Decisions

### Across all six areas

- **House patterns hold.** Controllers call services, services call repositories, no interfaces, the result wrapper, `LogResultAsync` for outcomes, static mappers, tiered DTOs, one type per file, no magic literals, UTC times. Every new action carries a rate-limit policy of its own.
- **Failure shapes do not grow.** New failures return the envelope; input validation keeps the framework's shape (ADR 0003).
- **Schema changes go through `dotnet ef migrations add`.** Existing posts and comments become `VISIBLE`.
- **New enums are stored as text,** the way the friendship status is.
- **New settings have defaults in their options class,** because the settings files are gitignored and CI writes its own. Each new setting is added to the prerequisites list in `CLAUDE.md`.
- **SQL Server refuses two cascading paths to the same table.** `PostLike` and `Report` each point at `User` and at something `User` owns. Where the database cannot cascade, the purge of soft-deleted users deletes the rows explicitly, as it already does for messages, friendships and posts.

### Images

- **One storage class** owns everything about files: checking an upload, saving it, deleting it, and turning a stored file into its address. Nothing else touches the disk. It is the single place to change if images later move to cloud storage.
- **Accepted:** JPEG, PNG and WebP, up to 5 MB each, up to 10 per post and 5 per message. The format is decided by the file's leading bytes, not by its name or declared type. These limits live in configuration.
- **File names are generated by the server.** The client's file name is never used in a path.
- **Addresses.** Files are served as static files from a path under `/api`, so only `/api` and `/hubs` cross the proxy (ADR 0001). The database stores the relative address. The upload folder is configurable and gitignored.
- **Message photos are not access-controlled.** They are protected by an unguessable name only, because an image tag cannot send the access token that lives in memory (ADR 0004).
- **`POST /api/posts` and `POST /api/messages/{receiverId}` become `multipart/form-data`,** replacing their JSON bodies. The request size limit on both is raised to fit the image limits, since the server's default is smaller than ten 5 MB photos.
- **A post** needs a title, and either text or at least one photo. **A message** needs text or at least one photo.
- **All or nothing.** If any file fails a check, nothing is saved and nothing is stored.
- **`PostImage` and `MessageImage`** hold the address and an order index that follows upload order.
- **Editing stays text-only.** `PUT /api/posts/{id}` and `PUT /api/messages/{id}` keep their JSON bodies. Photos are removed one at a time with `DELETE /api/posts/{postId}/images/{imageId}`, by the author or an administrator. Removing the last photo of a post with no text is refused, and so is editing a post's text to empty when it has no photos.
- **DTOs.** The standard and full post shapes, the three message shapes (sent, pushed, history) carry an ordered list of images (id, address, order). The conversation list entry carries the image count of the last message.
- **Deleting removes files.** Deleting a post, a message, a conversation (when a friendship is removed) or a purged user's content removes the files as well. File removal happens after the database change and is best-effort: a file that cannot be removed is logged and does not fail the request.

### Likes

- **`PostLike`,** unique per post and user.
- **`POST /api/posts/{postId}/likes`** likes, **`DELETE`** unlikes, **`GET`** returns the users who liked, paged, in the minimal user shape, readable by anyone who can read the post.
- **Refusals.** Liking twice, unliking a post one has not liked, and any like action on a post the caller cannot read (missing or hidden) fail with a message.
- **`PostLiked`** is pushed to the author's group, and not when the author likes their own post. Its payload names the post, the user who liked it and the new like count.
- **The standard and full post shapes gain** a like count, a comment count and a "liked by caller" flag, which is false for a guest.

### Feed ranking

- **Candidates** are every post the caller may read that the caller did not write.
- **Score** rises with connection and activity and falls with age. Connection has three tiers: friend, user with at least one mutual friend, other. Activity is likes plus comments. The weights and the decay are named constants.
- **Ranking and paging happen in one database query.** The feed never loads all posts into memory. Raw SQL is acceptable if the expression does not translate, as the conversation list already does.
- **Ties** are broken by newest id, so the order is deterministic.
- **Paging is by page number, as everywhere else.** Scores change over time, so a post can repeat or be skipped between two pages loaded minutes apart. That is accepted.

### Typing

- **`SendTyping(receiverId)`** is the hub's first client-called method. The hub stays free of logic: it passes the call to a service, which relays `UserTyping` with the sender's id to the receiver's group only if the two are accepted friends. Otherwise it does nothing and reports nothing.
- **Nothing is stored and nothing is logged.** There is no "stopped typing" event; the client hides the indicator after a short silence.

### Admin reads

- **A new admin controller,** administrator-only, at `api/admin`.
- **`GET /api/admin/logs`:** paged, newest first, filtered by user, authorized or not, succeeded or not, entity name, entity id, action and a date range. The empty log service is given this job and reads through the log repository, not the database context.
- **`GET /api/admin/statistics`:** one object with totals (active accounts, deactivated accounts, posts, comments, messages, likes, waiting reports), counts created in the last 7 and 30 days (accounts, posts, comments, messages), and users online now, taken from the connection tracker.
- **`GET /api/admin/users`:** paged list of accounts with their state, filtered by state (active, deactivated, soft-deleted) and by username. It is not in the documentation's endpoint list, but the admin panel cannot restore a deactivated account without it, because every other read hides such accounts.

### Reports and moderation

- **Moderation status** on `Post` and `Comment`: `VISIBLE`, `FLAGGED`, `HIDDEN`.
- **Visibility rule, applied in every read:** hidden content is returned only to its author and to administrators. For everyone else it is left out of every list (the feed, a user's posts, the comments of a post, the comments inside a full post) and reads as not found by id. Counts leave it out. Flagged content is treated as visible.
- **Post and comment shapes gain a hidden flag,** which only an author or administrator can ever see as true. The full status appears only in admin shapes, so a flag is never disclosed to ordinary users.
- **Hidden content cannot be liked, commented on or reported** by anyone who cannot read it. Its author can still edit or delete it; editing does not change the status.
- **`Report`** as the documentation models it, with a reason of bounded length.
- **`POST /api/reports`:** target type, target id, reason. The target must exist and be readable by the caller. Reporting oneself or one's own content is refused, and so is a second report of the same target while the caller's first is pending.
- **`GET /api/reports`:** administrator-only, paged, newest first, filtered by status (pending by default) and target type. Each row carries the reporter, the reason, the status, the reviewer and times, and a snapshot of the target: for a post its title, text, photos and author; for a comment its text, author and post; for a user the username and bio. A target that no longer exists is shown as missing.
- **`PUT /api/reports/{id}`:** administrator-only, with a decision of hide or reject.
  - **Hide** sets a post or comment to `HIDDEN`, or deactivates a reported user through the existing admin deactivation path. The report and every other pending report on the same target become `RESOLVED`, with reviewer and time, in one transaction.
  - **Reject** sets this report to `REJECTED` and changes nothing else.
  - A report that is not pending, or a hide on a target that no longer exists, is refused.
- **`PUT /api/admin/posts/{id}/moderation` and `PUT /api/admin/comments/{id}/moderation`:** administrator-only, set any of the three statuses. They do not touch reports. The n8n spec will later let its service identity call these two.
- **`GET /api/admin/moderation-queue`:** administrator-only, paged, flagged content of one type per call (posts or comments, chosen by a query parameter), with the same target snapshot as the report list.
- **Reports are not cleaned up when their target is deleted.** They show a missing target and can be rejected.

## Testing Decisions

- **What makes a good test here.** A test acts the way a client does (an HTTP request, or a hub call) and asserts on what a client would see: the status code, the response body, a file that can be fetched, a push that arrives. It does not reach into services, check which method ran, or assert on log rows.
- **Main seam: the existing HTTP integration seam.** The test host built on the web application factory, real requests, the real test database, reset and reseeded per class. Uploads are real multipart requests with small real image bytes built in the test, and an uploaded photo is proven by fetching its returned address.
- **Upload folder in tests.** The test host points the upload folder at a temporary directory through configuration, the same way it forces email off, and it is emptied between runs.
- **One new seam: a real SignalR client.** Tests connect a real client to the test host as a seeded user and assert on what that user receives. This covers push targets (`PostLiked` reaches the author and nobody else, a photo message arrives with its photos) and the client-called typing method (a friend receives `UserTyping`, a non-friend receives nothing). The helper is shared. Message-editing ticket 01 needs the same thing and must use the same helper, whichever of the two is implemented first; the commented-out hub-context replacement in the test host is not the chosen route.
- **Time.** Feed tests and statistics tests need posts of chosen ages. They get them from seed data with fixed creation times, not from a clock abstraction.
- **Feed ranking is tested by its properties,** not by its formula: with the other two factors equal, friend beats mutual beats stranger, more activity beats less, newer beats older. Own posts and other users' hidden posts never appear. A user with no friends gets a non-empty feed.
- **Visibility is tested from three callers:** another user (gone from lists, not found by id), the author (still there, marked hidden) and an administrator (still there).
- **Seed data.** New seed rows are appended; existing seeded ids do not change, because existing tests rely on them. Each test class keeps the comment that describes the seed state it uses.
- **Authorization** is tested on every administrator-only endpoint with an ordinary user and with a guest.
- **Not tested:** the purge of soft-deleted users (the test host removes hosted services), and docs.
- **Prior art.** The existing controller test classes: one class per controller, tests named `Method_Condition_ExpectedResult`, grouped by endpoint, with the shared authentication helper. There are no message, friendship or hub tests yet; the first ones arrive with this spec and with message-editing ticket 01.

## Out of Scope

- **n8n and everything behind it:** outbound webhooks, the API-key identity, the internal endpoints, `digestEmailEnabled`, AI moderation, automatic hiding after several reports, email through n8n. `ReceiveNotification` is built there too, with its only caller.
- **Cloud storage for images.** Local disk until after the deadline; the storage class is the swap point.
- **Access control on message photos** beyond an unguessable name.
- **Image processing:** resizing, thumbnails, stripping metadata.
- **Adding photos to an existing post or message.**
- **A push when a like is removed, when a message is deleted, or when content is hidden.**
- **Server-side throttling of typing signals.** The rate limiter does not cover hub calls.
- **Feed performance at scale,** and cursor paging.
- **The frontend pages** that use these endpoints.
- **The open backend tickets:** readiness 06 to 09, message-editing 01 and openapi-document 01. They stand on their own.

## Further Notes

- **Order that helps the frontend.** Images and likes change the post and message contracts, so they come before the feed and chat pages. Feed ranking needs likes. Reports and moderation need images only for the target snapshot.
- **The visibility rule touches reads that images, likes and feed ranking also touch.** Tickets should make moderation status arrive either before those reads are rewritten or as one sweep after, not interleaved.
- **A deliberate difference from the documentation:** an unexpected server error keeps the failure envelope instead of `ProblemDetails` (owner's decision, 2026-11-14).
- **An addition to the documentation:** `GET /api/admin/users`.
- **The OpenAPI document must still generate** with the multipart endpoints once openapi-document ticket 01 is done.
- **Docs to update as behavior lands:** `docs/backend/controllers.md`, `data.md`, `signalr.md` (the hub is no longer push-only), `background-jobs.md` (the purge), `README.md` and the prerequisites in `CLAUDE.md`.
