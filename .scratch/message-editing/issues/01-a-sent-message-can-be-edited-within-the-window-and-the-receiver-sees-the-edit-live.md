# 01: A sent message can be edited within the window, and the receiver sees the edit live

**What to build:** A user who has just sent a message can correct it for a limited time (the edit window, 20 minutes today), and the friend they sent it to sees the corrected text appear without reloading the conversation. Neither works today.

The edit window is applied backwards: a message that was just sent is rejected with the "edit window expired" message, and a message older than the window can be edited forever. The intent, shown by the window constant's name and by that failure message, is the opposite: editable while the message is younger than the window, rejected afterwards.

The `MessageEdited` push goes to the editor's own SignalR group. It must go to the message's receiver, and only to the receiver (decided with the owner on 2026-11-13). The editor already gets the edited message in the response to the edit request; the receiver is the one whose screen is stale. This also matches the other message events: `ReceiveMessage` goes to the receiver and `MessageSeen` goes to the other user in the conversation. The editor's other tabs or devices are not notified, just as a sender's other devices are not told about a message they sent.

The existing rules stay as they are: only the sender can edit, the receiver and outsiders are refused, an unknown message is not found, and the payload of `MessageEdited` does not change.

There are no integration tests for messages, and no test yet asserts which SignalR group a push was sent to. This ticket adds both. The implementer chooses how to observe the push (the test host has an unused, commented-out replacement of the hub context that hints at one way); the choice should be one later real-time tests can reuse. A test also needs a message that is older than the edit window, which the current seed data does not provide.

Out of scope: writing a `Log` row for a successful edit (the owner decided edits are not logged), and pushing anything when a message is deleted.

The docs currently record the inverted window as a possible bug and name the editor's group as the push target. They are corrected in the same change.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Difficulty:** tricky: real-time behaviour with no existing test pattern for asserting where a SignalR push went; the implementer has to choose that seam.

- [ ] Editing a message younger than the edit window succeeds and returns the edited message, marked as edited
- [ ] Editing a message older than the edit window fails with the "edit window expired" message
- [ ] A successful edit pushes `MessageEdited` to the receiver's group and to no other group, and a failed edit pushes nothing
- [ ] Integration tests cover both window cases, the push target, and the existing refusals (receiver, outsider, unknown message)
- [ ] `docs/backend/signalr.md`, `docs/backend/controllers.md` and `README.md` no longer carry the possible-bug note and state the receiver as the target of `MessageEdited`

## Comments

2026-11-14, from the documented-backend-features spec:

- The way to observe a push is now decided with the owner: tests connect a real SignalR client to the test host and assert on what each connected user receives. The commented-out hub-context replacement in the test host is not the route.
- The shared helper is built by ticket 01 of `.scratch/documented-backend-features/issues/`. Implement that ticket first and use its helper here. If this ticket is taken first anyway, build the helper to that ticket's acceptance criteria.
