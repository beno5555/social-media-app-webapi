# 01: Real-time tests connect a real SignalR client

**What to build:** The integration suite can prove what a connected user receives over the hub. A test connects a real SignalR client to the test host as a seeded user, acts through HTTP as another user, and asserts on the events that arrive. Nothing in the suite can do this today, and three later tickets in this spec (likes, message photos, typing) need it, as does message-editing ticket 01.

This ticket builds the shared helper and proves it with the first hub test: when one seeded friend sends the other a text message, the receiver's client gets `ReceiveMessage` with that message, and a third connected user gets nothing.

The seam was chosen with the owner on 2026-11-14 (see Testing Decisions in `.scratch/documented-backend-features/spec.md`). The commented-out replacement of the hub context in the test host is not the chosen route and is removed.

The helper must also serve a test that calls a hub method from the client, which ticket 07 needs.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Difficulty:** tricky: a new test pattern (a SignalR client against the in-memory test host, signed in with a JWT) that later tickets reuse, with timing to get right.

- [ ] A shared test helper connects a SignalR client to the test host as any seeded user, signed in the way a browser client is
- [ ] A test can wait for a named event and read its payload, and fails with a clear message if the event does not arrive in time
- [ ] A test can assert that a connected user received no event, without a long fixed wait
- [ ] A test can call a hub method from the client
- [ ] Sending a text message through HTTP delivers `ReceiveMessage` to the receiver's client with the message's content and sender
- [ ] A third connected user receives nothing for that message
- [ ] The commented-out hub-context replacement is gone from the test host
- [ ] The Testing section of `docs/backend/signalr.md` describes this seam in place of the historical note
