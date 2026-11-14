# What's next

The order of work toward the full-stack deadline, 2026-11-25. Tick a step when it is done, and add new steps here. Decisions live in `docs/adr/`, rules in the conventions skill, and work items in the tickets; this file holds the order and the list of what is still missing.

**Scope is fixed.** The documentation sent to the principal (`webcity-documentation.md` at the repo root) is the commitment: everything in it is due on the 25th. The old step "decide what ships" is therefore closed. The frontend comes first, because none of it exists; backend gaps are built in parallel, just before the page that needs them.

The schedule below has one buffer day and no other slack. If a day slips, the items under **Last in line** move, not the pages.

## Awaiting the owner's review

Work merged to `master` while the owner was away. It passed the full integration suite and a spec review, but the owner has not looked at it yet. Review each item, then delete its line.

- **Documented-backend-features ticket 02: a post can be created with photos** (gap A, first part), merged 2026-11-14. Look at:
  - the new `ImageStorage` (`backend/aspnetproject/Infrastructure/Services/Storage/`), the only code that touches files;
  - the contract change: `POST /api/posts` is now `multipart/form-data`, and every post read returns `Images`;
  - the open points at the bottom of the ticket, under Comments.

## Open decisions

Settle these before the spec session that needs them. Each has a going-in recommendation.

1. **Who sends email.** The document says n8n sends every transactional email; today MailKit does. Recommendation: move it in step 9, and leave MailKit working until then.
2. **Admin and developer channels.** Telegram for both, which needs one bot token and a chat id from the owner.
3. **What a guest sees.** Today a signed-out visitor can read a profile, a user's posts, one post and its comments. Recommendation: the frontend shows exactly that, read-only, and sends every other route to sign-in.
4. **Where the demo runs.** On the owner's machine (dev proxy, n8n in Docker) or on a host behind nginx (ADR 0001). A host costs most of a day and must come out of step 10.

Settled on 2026-11-14:

- **An unexpected server error keeps the failure envelope.** The document says `ProblemDetails`; the owner accepts the difference (readiness ticket 02 and ADR 0003 stand).
- **AI moderation uses OpenAI's moderation endpoint.** The owner provides the API key before step 9.
- **Hiding a reported user means deactivating the account.** For a `USER` report, the admin's "hide" decision deactivates that user.
- **Images go on local disk.** JPEG, PNG and WebP; 5 MB each; up to 10 per post and 5 per message. AWS S3 was considered and left for after the deadline.
- **Real-time behaviour is tested with a real SignalR client** connected to the test host.

## Now

Dates are the day a step should finish.

1. [ ] **Sat 14: design session and scaffold.** Timebox the design session to half a day. It chooses the styling toolset and the visual identity (palette, type, shape, mood).
   - Going-in recommendation for the toolset: Tailwind with Angular CDK, because the look must not be ordinary.
   - Fixed constraint: the typeface must cover Latin, Cyrillic and Georgian.
   - Consider installing one general frontend design skill first (candidate: Anthropic's `frontend-design`). Read `docs/agents/skills.md` before installing.
   - Output: the token values, a short style brief, the Styling rules in §10 of the conventions skill's TypeScript reference (replacing its temporary line), and an ADR for the toolset.
   - Then scaffold `frontend/` with the dev proxy for `/api` and `/hubs`.
2. [ ] **Write the tickets, one group at a time.** For each group the owner runs `/to-spec` and then `/to-tickets` in a session that already holds the context, because `/to-spec` only writes down what the conversation has settled.
   - Done 2026-11-14: the backend gaps A to F, as 13 tickets in `.scratch/documented-backend-features/issues/`. Tickets 01, 02 and 03 can start at once; the longest chain is 02 → 05 → 08 → 09 → 11 → 12.
   - Sat 14: the frontend pages of steps 4 to 8. The session first asks the owner the questions under **Questions for the frontend spec session** below, then the owner runs `/to-spec` and `/to-tickets`.
   - By Fri 20: n8n, gaps G and H with the six workflows.
3. [ ] **Any time, in parallel: the open backend tickets.** Readiness 06, 08 and 09, and openapi-document 01. Ticket 08 bites as soon as sign-in exists, 09 as soon as a date is shown, and the document promises per-user limits (06) and Swagger. Message-editing 01 (the 20-minute edit window) was done on 2026-11-14.
4. [ ] **Mon 16: shell and auth.** Lazy-loaded feature areas, the HTTP interceptor with automatic refresh, session restore, route guards for signed-in users and administrators, and the pages: register, sign in, password reset, account reactivation.
   - Backend alongside: **images** and **likes** (gaps A and B). Images ticket 02 (photos on a new post) is done and awaiting review. They change the post and message contracts, so they land before the pages that use them.
5. [ ] **Tue 17: feed, posts and comments.** Feed with pagination, create a post with photos, edit and delete, like, comment, edit and delete a comment, report a post or comment.
   - Backend alongside: **feed ranking** (gap C).
6. [ ] **Wed 18: profile, friends and search.** Profile page (bio, posts, friends), edit profile, user search, friend requests (send, accept, decline, remove; the three lists), account settings (deactivate, delete, digest opt-out), the guest view.
   - Backend alongside: **typing on the hub** (gap D), done on 2026-11-14 (ticket 07).
7. [ ] **Fri 20: chat and live notifications.** The SignalR service and the central state (signals) for the session, conversations, online friends and unread count. Conversation list with last message, conversation with photos, edit and delete, seen, typing, presence and last active. Toasts for a new message, comment, like and a friend coming online.
   - Backend alongside: **admin logs and statistics**, **reports and moderation** (gaps E and F).
8. [ ] **Sat 21: admin panel.** Users (create, edit, deactivate, restore, delete, grant and remove the administrator role), delete any post or comment, logs, statistics, the reports list, the moderation queue.
9. [ ] **Mon 23: n8n.** One day for the plumbing and two workflows, one day for the other four.
   - Sun 22: gap G, then the transactional-email and exception-alert workflows.
   - Mon 23: AI moderation, report handling, weekly digest, birthday notifications.
   - The workflows are exported as JSON into the repo, with a compose file that starts n8n.
   - Needs from the owner before Sun 22: Docker Desktop installed (it is not on this machine as of 2026-11-14), the OpenAI key, the Telegram bot token and chat id, and SMTP credentials entered in n8n.
10. [ ] **Tue 24: buffer and finish.** Responsive pass on a phone width. Walk the document top to bottom against the running app and fix what fails. Update `README.md` (monorepo ticket 04) and `docs/backend/*.md` for everything added.
11. [ ] **Wed 25: hand in.**

### Questions for the frontend spec session

A session that is asked to prepare the frontend spec asks the owner these, one at a time and with a recommendation each, before `/to-spec`. The document's "user interface" section and ADRs 0001 and 0003 to 0006 are the starting point; the owner is strong in C# and rusty in Angular.

1. **The page list and the routes.** Which pages exist and at which addresses, including the four auth pages, the feed, a single post, a profile, profile settings, search, friend requests, chat and the admin pages.
2. **What a guest sees** (open decision 3), and where a guest lands on the home address.
3. **The app shell.** What the navigation holds on a desktop and on a phone, and where the unread count, the live notifications and the search box sit.
4. **Chat on a phone.** One screen with the list and the conversation side by side, or two screens.
5. **The admin panel.** One page with tabs or a page per area (users, logs, statistics, reports, moderation queue).
6. **Languages on the 25th.** Which dictionary files ship (ADR 0006).
7. **Where the demo runs** (open decision 4), because a hosted demo changes how the frontend is built and served.

### Days the owner is away

The owner expects to lose more than a day, which is more than the buffer. The backend tickets (step 3 and gaps A to F) are the work that can run without the owner: they are covered by integration tests and can be implemented in a cloud session and reviewed afterwards. The pages cannot, because someone has to look at them. So the backend tickets are written first, on Sat 14, and a day away is spent on them, not lost.

### Last in line

If time runs short, these are finished last, in this order from the end: the weekly digest, birthday notifications, grouping of repeated exception alerts, automatic hiding after several reports, the mutual-friends tier of the feed ranking, the activity part of the statistics page.

## What the document promises and the backend lacks

Checked against the code on 2026-11-14. Everything not listed here already exists (auth, users, accounts, posts and comments as text, friendships, text chat with seen and unread, presence, rate limiting, cleanup jobs, integration tests). Gaps A to F are specified in `.scratch/documented-backend-features/spec.md`; G and H wait for the n8n spec.

- **A. Images.** No upload exists anywhere. Needed: storage on disk with format and size checks, static serving, `PostImage` and `MessageImage`, `POST /api/posts` and `POST /api/messages/{receiverId}` as `multipart/form-data`, `DELETE /api/posts/{postId}/images/{imageId}`, image URLs in the post and message DTOs and in the `ReceiveMessage` push.
- **B. Likes.** `PostLike`, the three `/api/posts/{postId}/likes` endpoints, the `PostLiked` push to the author, and a like count and "liked by me" on the post DTOs.
- **C. Feed ranking.** The feed today is friends' posts only, newest first. Needed: friends' and other users' posts, ranked by connection (friends, then users with mutual friends), activity (likes and comments) and age.
- **D. Hub.** `SendTyping` from the client and the `UserTyping` push. Done on 2026-11-14.
- **E. Admin reads.** `GET /api/admin/logs` (`LogService` exists but is empty and nothing calls it), `GET /api/admin/statistics`, and a list of accounts in any state, which the document does not list but restoring a deactivated account needs.
- **F. Reports and moderation.** The `Report` entity and its three endpoints; `moderationStatus` on `Post` and `Comment`, with hidden content filtered from every read that other users make; `GET /api/admin/moderation-queue` and the two `PUT /api/admin/{posts|comments}/{id}/moderation` endpoints.
- **G. n8n plumbing.** Outbound webhooks signed with a shared secret for registration, post and comment creation, a new report and an exception (from requests and from background jobs). An API-key identity that can reach only the internal and moderation endpoints. `GET /api/internal/digest`, `GET /api/internal/birthdays`, `POST /api/internal/notifications` with the `ReceiveNotification` push it sends. `digestEmailEnabled` on `User`, editable in the profile settings.
- **H. Email through n8n.** The welcome, password-reset, account-reactivation and security emails leave the backend as events instead of being sent by MailKit (open decision 1).

## After the deadline

- A few end-to-end test flows (sign in, post, message). They come before component tests.
- Russian and Spanish dictionary files (ADR 0006).
- Translated server text, which needs a stable code on every backend failure (ADR 0006).
- **Share one test host per test class**, once the integration suite reaches about 400 tests or a run takes more than a minute (on 2026-11-14: 159 tests, 18 s). xUnit builds a new class instance per test, so each test boots its own `CustomWebApplicationFactory`. Moving the factory into an `IClassFixture` would roughly halve the run time; the database reset per test stays.
