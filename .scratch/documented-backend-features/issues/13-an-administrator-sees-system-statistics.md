# 13: An administrator sees system statistics

**What to build:** An administrator opens one statistics view and sees how big and how active WebCity is. `GET /api/admin/statistics` is administrator-only and returns one object:

- **Totals:** active accounts, deactivated accounts, posts, comments, messages, likes, and reports waiting for a decision.
- **Activity:** how many accounts, posts, comments and messages were created in the last 7 days and in the last 30 days.
- **Online now:** how many users have a live hub connection, taken from the connection tracker.

Totals count every row whatever its moderation status. A successful read writes no `Log` row.

The endpoint joins the admin controller from ticket 03 if that ticket is done; it does not depend on it.

Decisions are under "Admin reads" in `.scratch/documented-backend-features/spec.md`.

**Blocked by:** 01 (Real-time tests connect a real SignalR client), 05 (A post can be liked), 11 (A user can report a post, a comment or a user, and an administrator sees the reports)

**Status:** ready-for-agent

**Difficulty:** routine: counts over existing tables returned in one shape; the online count is tested with the helper from ticket 01.

- [ ] An administrator gets the totals, and they match the seed data
- [ ] Deactivated accounts are counted separately from active ones
- [ ] The 7-day and 30-day counts include rows created inside each window and leave out older ones, using seed rows with fixed creation times
- [ ] The count of waiting reports includes pending reports only
- [ ] The online count rises when a seeded user connects a SignalR client and falls when they disconnect
- [ ] An ordinary user and a guest are refused
- [ ] The endpoint has its own rate-limit policy and a successful read writes no `Log` row
- [ ] `docs/backend/controllers.md` and `README.md` describe the statistics
