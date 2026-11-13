# 06: A message can carry photos

**What to build:** A user sends a friend up to 5 photos in a message, with or without text. The photos arrive live with the message, stay in the conversation's history, and are removed from the server when the message, the conversation or the account goes away. Today a message is text only.

Sending changes from a JSON body to `multipart/form-data`. A message needs text or at least one photo. The same checks as for post photos apply, through the storage class built in ticket 02, and a message with one bad file is refused as a whole. The rules about who may message whom do not change.

- The sender's confirmation, the `ReceiveMessage` push and the conversation history each carry the message's photos in order.
- The conversation list entry carries the image count of the last message, so the client can show something for a photo with no text. That list is built with raw SQL.
- Editing a message stays a JSON, text-only request. Photos of a sent message cannot be changed.
- Message photos are served the same way as post photos and are protected by their unguessable name only (decided in the spec, because an image tag cannot send the access token).
- Files are removed when a message is deleted, when a friendship is removed (which deletes the conversation), and when the purge of soft-deleted users deletes messages. File removal is best-effort after the database change. The purge path is not covered by a test: verify it by reading and one manual run, and record that on this ticket.

There are almost no message tests today (ticket 01 adds the first). This ticket adds the tests for sending.

Decisions are under "Images" in `.scratch/documented-backend-features/spec.md`.

**Blocked by:** 01 (Real-time tests connect a real SignalR client), 02 (A post can be created with photos)

**Status:** ready-for-agent

**Difficulty:** tricky: the live push and the raw-SQL conversation list both change, and file cleanup reaches three delete paths.

- [ ] A message with text and photos, with photos only, and with text only is each accepted between friends
- [ ] A message with neither text nor photos is refused
- [ ] A wrong-format file, an oversized photo and more than 5 photos are each refused, and then no message is saved and no file is stored
- [ ] The sender's response lists the message's photos in upload order
- [ ] The receiver's connected client gets `ReceiveMessage` with the photos, and their addresses serve the files
- [ ] The conversation history returns each message's photos
- [ ] The conversation list entry gives the image count of the last message
- [ ] Sending to a non-friend, to oneself or to an unknown user is refused as before
- [ ] Deleting a message removes its photo rows and files
- [ ] Removing a friendship removes the files of the conversation it deletes
- [ ] The purge of soft-deleted users removes the files of the messages it deletes, verified by a manual run recorded on this ticket
- [ ] The request size limit on sending fits five photos at the size limit
- [ ] `docs/backend/controllers.md`, `docs/backend/data.md`, `docs/backend/signalr.md` and `README.md` describe photos in messages
