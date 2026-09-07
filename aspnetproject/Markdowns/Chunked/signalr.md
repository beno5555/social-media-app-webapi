# AspNetProject — SignalR

> Part of the AspNetProject doc set. See also: `README.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

---

## Architecture

SignalR (not raw WebSockets) — automatic transport fallback, simpler API than managing raw `WebSocket` instances.

`MessageHub`, in `/Hubs` (sibling to `/Controllers`, its own transport-layer tier). Mapped at `hubs/messages` (`app.MapHub<MessageHub>("hubs/messages")`).

- Push-only. No client-invokable methods. All validation stays in the relevant controller/service (`MessageController`/`MessageService`, `CommentController`/`CommentService`), not the hub.
- One SignalR group per `userId` (one-on-one messaging only — no conversation/group table). Connection added to its group in `OnConnectedAsync`; SignalR removes it from all groups automatically on disconnect.
- `IHubContext<MessageHub>` is injected directly into `MessageService` **and now also `CommentService`** — both treat "mutate + notify" as one atomic unit per service method.
- `MessageHub` now also depends on `UserConnectionTracker` and `PresenceService` to track and broadcast online/offline state:

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

## Auth

Hub requires `[Authorize]`, same JWT scheme as REST.

Browsers can't set custom headers on a WebSocket/SSE handshake, so the client sends the JWT via query string (`?access_token=...`). `JwtBearerEvents.OnMessageReceived` (now in `AuthenticationExtensions.cs`, not inline in `Program.cs`) reads `access_token` from the query string specifically for requests under `/hubs` — REST endpoints are unaffected, still header-only.

## CORS

Policy name: `SignalRTestPolicy` (`CorsExtensions.AddCorsPolicies`), applied globally via `app.UseCors("SignalRTestPolicy")` (not scoped only to the hub).

- Explicit origin allowlist via `WithOrigins(...)` — `AllowAnyOrigin()` cannot be combined with `AllowCredentials()`.
- Origins stored in `appsettings.{Environment}.json` under `Cors:AllowedOrigins`.
- `UseCors(...)` is registered before `UseRateLimiter`/`UseAuthentication`/`UseAuthorization`/`MapHub` in the middleware pipeline.

## DTOs

| DTO | Used for | Notes |
|---|---|---|
| `SentMessageDto` | Sender's own REST confirmation | Id, Content, SentAt |
| `PushMessageDto` | Receiver's live hub arrival (`ReceiveMessage`) | Id, Content, Sender, SentAt |
| `StandardMessageDto` | Full conversation history (`GetConversation`) | Id, Content, Sender, ReceiverId, SentAt, Seen, SeenAt, IsEdited |
| `MessageEditedDto` | Edit push (`MessageEdited`) | Id, Content |
| `NotifyCommentDto` | Live comment push (`ReceiveComment`) | Id, AuthorUsername, Content |

## Hub events (server → client push)

| Event | Trigger | Payload | Target group |
|---|---|---|---|
| `ReceiveMessage` | `SendMessage` persists successfully | `PushMessageDto` | Receiver's group |
| `MessageSeen` | Conversation marked read (last unread message from the other user, seen) | `{ SeenBy, SeenAt }` | Sender's group |
| `MessageEdited` | Message edit persists successfully | `MessageEditedDto` | Editor's own group |
| `ReceiveComment` | A comment is added to a post, by someone other than the post's author | `NotifyCommentDto` | Post author's group |
| `UserOnline` | A user's first connection opens | `{ UserId }` | Each accepted friend's group |
| `UserOffline` | A user's last connection closes | `{ UserId, LastActiveAt }` | Each accepted friend's group |

No client-invokable hub methods (server ← client) exist. Typing indicators remain the earmarked future use case for that pattern.

## Read receipts

`Message.SeenAt` (nullable `DateTime`) plus `Seen` (redundant-but-harmless boolean, updated atomically alongside `SeenAt` in the same `ExecuteUpdateAsync`).

`MessageRepository.MarkConversationAsReadAsync(readerId, otherUserId)`:
- Only marks messages and returns `(marked: true, seenAt)` if the **last message** in the conversation was sent by `otherUserId` and was unread.
- Guards against false-positive `MessageSeen` pushes when a reader reopens an already-read conversation.
- Bulk-updates all unread incoming messages from that sender, not just the last one.
- Called automatically from `MessageService.GetConversationAsync` whenever a conversation is fetched — the client doesn't need to call the mark-as-read endpoint separately just from viewing, though `PUT .../mark-as-read` still exists as an explicit action.

## Testing

Note: the earlier local-HTML-page + `@microsoft/signalr` + Live Server manual two-tab test approach (and the echo-test script) predates presence/comment-push and hasn't been re-verified against them — treat as historical context for the messaging-only hub, not a current test plan.

## Not implemented / explicitly deferred

- **Typing indicators** — earmarked as the correct future use case for a client-invokable hub method; not built yet.
- **`EditedAt` timestamp** on `MessageEditedDto` — deferred to frontend work; `Message.IsEdited` (computed from `LastUpdatedAt`) is available server-side if needed sooner.
