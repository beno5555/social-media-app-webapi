# 07: A friend sees that the user is typing

**What to build:** While a user is writing to a friend, the friend's client is told, so it can show "typing...". The user's client calls `SendTyping` on the hub with the friend's id, and the friend receives `UserTyping` with the sender's id.

This is the hub's first method that a client calls; until now the hub only pushes. The rule that hubs hold no logic stands: the hub passes the call to a service, and the service relays the signal only if the two users are accepted friends. In every other case (not friends, unknown user, oneself) it does nothing and reports nothing to the caller.

Nothing is stored and nothing is logged. There is no "stopped typing" event; the client hides the indicator after a short silence.

Decisions are under "Typing" in `.scratch/documented-backend-features/spec.md`. Server-side throttling of the signal is out of scope.

**Blocked by:** 01 (Real-time tests connect a real SignalR client)

**Status:** done

**Difficulty:** routine: small and fully specified, and the test helper from ticket 01 already covers calling a hub method and asserting who received what.

- [x] When a user's client calls `SendTyping` for an accepted friend, the friend's connected client receives `UserTyping` with the sender's id
- [x] No other connected user receives it
- [x] A call that names a non-friend, an unknown user or the caller delivers nothing and raises no error for the caller
- [x] The hub method contains no checks of its own; the friendship check lives in a service
- [x] No `Log` row and no database write results from a typing signal
- [x] `docs/backend/signalr.md` no longer calls the hub push-only and lists `SendTyping` and `UserTyping`; `README.md` mentions the typing indicator
