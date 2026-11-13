# WebCity — SignalR

> Part of the WebCity doc set. See also: `README.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

---

## Architecture

SignalR (not raw WebSockets) — automatic transport fallback, simpler API than managing raw `WebSocket` instances.

`MessageHub`, in `/Hubs` (sibling to `/Controllers`, its own transport-layer tier). Mapped at `hubs/messages` (`app.MapHub<MessageHub>("hubs/messages")`).

- Mostly push. The one client-called method is `SendTyping` (see Typing). The hub holds no logic: all validation stays in the relevant controller/service (`MessageController`/`MessageService`, `CommentController`/`CommentService`, `TypingService`), not the hub.
- One SignalR group per `userId` (one-on-one messaging only — no conversation/group table). Connection added to its group in `OnConnectedAsync`; SignalR removes it from all groups automatically on disconnect.
- `IHubContext<MessageHub>` is injected directly into `MessageService` **and now also `CommentService`** — both treat "mutate + notify" as one atomic unit per service method.
- `MessageHub` now also depends on `UserConnectionTracker` and `PresenceService` to track and broadcast online/offline state, and on `TypingService` for `SendTyping` (left out of the excerpt below):

```csharp
[Authorize]
public class MessageHub : Hub
{
    private readonly UserConnectionTracker _userConnectionTracker;
    private readonly PresenceService       _presenceService;

    public override async Task OnConnectedAsync()
    {
        int userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());

        var isFirstConnection = _userConnectionTracker.AddConnection(userId);
        if (isFirstConnection)
        {
            await _presenceService.NotifyUserOnlineAsync(userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();

        var wasLastConnection = _userConnectionTracker.RemoveConnection(userId);
        if (wasLastConnection)
        {
            await _presenceService.NotifyUserOfflineAsync(userId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

## Presence

- `UserConnectionTracker` (singleton, `ConcurrentDictionary<int, int>`) counts live connections per user — a user can have multiple tabs/devices open; presence should only flip on the *first* connect / *last* disconnect, not every socket event.
- `PresenceService`:
  - `NotifyUserOnlineAsync(userId)` — marks `User.LastOnlineAt = null` (the "online" sentinel, via `UserRepository.MarkActiveAsync`, a bulk `ExecuteUpdateAsync`), fetches the user's accepted friend ids, broadcasts `UserOnline { UserId }` to each friend's group.
  - `NotifyUserOfflineAsync(userId)` — stamps `User.LastOnlineAt = DateTime.UtcNow` (`MarkUserOfflineAsync`), broadcasts `UserOffline { UserId, LastActiveAt }` to friends.
- `User.LastOnlineAt`: `null` means online; a timestamp means "last seen at." `DisplayFriendDto.IsActive => LastActiveAt == null` mirrors this convention client-side.

## Typing

- The client calls `SendTyping(receiverId)` while its user writes to a friend. The hub passes the caller's id and `receiverId` to `TypingService.RelayTypingAsync`.
- The service pushes `UserTyping` (`UserTypingDto { UserId }`, the sender's id) to the receiver's group only if the two are accepted friends. A non-friend, an unknown user or oneself gets nothing, and the caller gets no error.
- Nothing is stored and nothing is logged. There is no "stopped typing" event; the client hides the indicator after a short silence. The server does not throttle the signal.

## Auth

Hub requires `[Authorize]`, same JWT scheme as REST.

Browsers can't set custom headers on a WebSocket/SSE handshake, so the client sends the JWT via query string (`?access_token=...`). `JwtBearerEvents.OnMessageReceived` (now in `AuthenticationExtensions.cs`, not inline in `Program.cs`) reads `access_token` from the query string specifically for requests under `/hubs` — REST endpoints are unaffected, still header-only.

## CORS

Policy name: `OriginPolicy` (`CorsExtensions.AddCorsPolicies`), applied globally via `app.UseCors("OriginPolicy")` (not scoped only to the hub).

- Explicit origin allowlist via `WithOrigins(...)` — `AllowAnyOrigin()` cannot be combined with `AllowCredentials()`.
- Origins stored in `appsettings.{Environment}.json` under `Cors:AllowedOrigins`. The same list decides which password-reset return addresses are accepted (see `auth.md`).
- `UseCors(...)` is registered before `UseRateLimiter`/`UseAuthentication`/`UseAuthorization`/`MapHub` in the middleware pipeline.

## DTOs

| DTO | Used for | Notes |
|---|---|---|
| `SentMessageDto` | Sender's own REST confirmation | Id, Content, SentAt |
| `PushMessageDto` | Receiver's live hub arrival (`ReceiveMessage`) | Id, Content, Sender, SentAt |
| `StandardMessageDto` | Full conversation history (`GetConversation`) | Id, Content, Sender, ReceiverId, SentAt, Seen, SeenAt, IsEdited |
| `MessageEditedDto` | Edit push (`MessageEdited`) | Id, Content |
| `NotifyCommentDto` | Live comment push (`ReceiveComment`) | Id, AuthorUsername, Content |
| `UserTypingDto` | Typing signal (`UserTyping`) | UserId (the sender) |

## Hub events (server → client push)

| Event | Trigger | Payload | Target group |
|---|---|---|---|
| `ReceiveMessage` | `SendMessage` persists successfully | `PushMessageDto` | Receiver's group |
| `MessageSeen` | Conversation marked read (last unread message from the other user, seen) | `{ SeenBy, SeenAt }` | Sender's group |
| `MessageEdited` | Message edit persists successfully | `MessageEditedDto` | Receiver's group only (the editor gets the edited message in the HTTP response) |
| `ReceiveComment` | A comment is added to a post, by someone other than the post's author | `NotifyCommentDto` | Post author's group |
| `UserOnline` | A user's first connection opens | `{ UserId }` | Each accepted friend's group |
| `UserOffline` | A user's last connection closes | `{ UserId, LastActiveAt }` | Each accepted friend's group |
| `UserTyping` | An accepted friend calls `SendTyping` for this user | `UserTypingDto` | Receiver's group only |

## Hub methods (client → server)

| Method | Arguments | Effect |
|---|---|---|
| `SendTyping` | `receiverId` (int) | Relays `UserTyping` to the receiver if the two are accepted friends; otherwise does nothing and reports nothing |

## Read receipts

`Message.SeenAt` (nullable `DateTime`) plus `Seen` (redundant-but-harmless boolean, updated atomically alongside `SeenAt` in the same `ExecuteUpdateAsync`).

`MessageRepository.MarkConversationAsReadAsync(readerId, otherUserId)`:
- Only marks messages and returns `(marked: true, seenAt)` if the **last message** in the conversation was sent by `otherUserId` and was unread.
- Guards against false-positive `MessageSeen` pushes when a reader reopens an already-read conversation.
- Bulk-updates all unread incoming messages from that sender, not just the last one.
- Called automatically from `MessageService.GetConversationAsync` whenever a conversation is fetched — the client doesn't need to call the mark-as-read endpoint separately just from viewing, though `PUT .../mark-as-read` still exists as an explicit action.

## Testing

Integration tests connect a real SignalR client to the test host and assert on what each connected user receives. The hub context is the real one; nothing is mocked. Example: `Hubs/MessageHubTests` in the test project.

`Helpers/HubTestClient` (test project) is the shared helper:

- `HubTestClient.ConnectAsync(factory, username)` signs a seeded user in through `api/auth/login` and connects over WebSockets to `hubs/messages` the way a browser client does: the token goes in an `Authorization` header on negotiate, and as `?access_token=...` on the WebSocket. Before it returns, it calls the hub once and waits for the reply. The server confirms a connection before `OnConnectedAsync` has added it to the user's group, but reads the client's calls only after that, so the reply proves the client is in its group.
- From the moment it connects, it records every event listed in `Helpers/HubEvents.All`. A test acts first (usually through HTTP as another user) and reads afterwards, so no push is lost. Add each new server event to `HubEvents`.
- `WaitForEventAsync<T>(eventName)` returns the oldest unread payload of that event. It fails with a message naming the user and the event if nothing arrives within 5 seconds.
- `AssertNoEventAsync(eventName)` fails if that event has arrived. It uses a marker, not a fixed wait. The helper pushes a marker event to this one connection through the real `IHubContext<MessageHub>`, then waits for it. Earlier events are ahead of the marker on the wire, and the client runs event handlers one at a time in arrival order, so once the marker arrives they have been recorded. A call's reply is not a substitute: the .NET client handles replies on a different loop from events.
- `InvokeAsync(methodName, args...)` calls a hub method as the user and waits up to 5 seconds for the server to finish it. Method names live in `Helpers/HubMethods`.

Dispose each client with `await using`, and the factory in the test class's `DisposeAsync`, so no connection or host outlives its test.

## Not implemented / explicitly deferred

- **`EditedAt` timestamp** on `MessageEditedDto` — deferred to frontend work; `Message.IsEdited` (computed from `LastUpdatedAt`) is available server-side if needed sooner.
