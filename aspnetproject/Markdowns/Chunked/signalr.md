# AspNetProject — SignalR

> Part of the AspNetProject doc set. See also: `core.md`, `data.md`, `auth.md`, `controllers.md`.

---

## Architecture

SignalR (not raw WebSockets) — automatic transport fallback (WebSocket → SSE → long polling), simpler API than managing raw `WebSocket` instances.

`MessageHub`, in `/Hubs` folder (sibling to `/Controllers` — not a controller, not a service, its own transport-layer tier).

- Push-only. No client-invokable methods currently. All validation stays in `MessageController`/`MessageService`, not the hub — consistent with keeping authorization/ownership logic in the service layer rather than the transport layer.
- One SignalR group per `userId` (not per-conversation, since the app is one-on-one messaging only — no conversation/group table exists in the schema). Connection added to its group in `OnConnectedAsync`; SignalR removes it from all groups automatically on disconnect, no manual cleanup needed.
- `IHubContext<MessageHub>` is injected directly into `MessageService`, not the controllers — keeps "mutate + notify" as one atomic unit per service method.

```csharp
[Authorize]
public class MessageHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserId().ToString());
        await base.OnConnectedAsync();
    }
}
```

## Auth

Hub requires `[Authorize]`, same JWT scheme as REST.

Browsers can't set custom headers on a WebSocket/SSE handshake, so the client sends the JWT via query string (`?access_token=...`) instead. `JwtBearerEvents.OnMessageReceived` is configured to read `access_token` from the query string specifically for requests under `/hubs` — REST endpoints are unaffected, still header-only.

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(accessToken) &&
            context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

## CORS

Required for local dev testing (Live Server on a different origin/port than the API) and for the SignalR negotiate step, which relies on credentials.

- Explicit origin allowlist via `WithOrigins(...)` — `AllowAnyOrigin()` cannot be combined with `AllowCredentials()`, browsers reject that combination outright.
- Origins stored in `appsettings.{Environment}.json` under `Cors:AllowedOrigins`, not hardcoded — dev origins (Live Server, local frontend dev servers) never need to touch or accidentally ship in production config.
- `UseCors(...)` must be registered before `UseAuthentication`/`UseAuthorization`/`MapHub` in the middleware pipeline.

## DTOs

Each scoped to exactly what its audience needs at that moment — avoids shipping fields that are always-false/irrelevant at send-instant, or duplicating fields the recipient already has.

| DTO | Used for | Fields |
|---|---|---|
| `SentMessageDto` | Sender's own REST confirmation | Id, Content, SentAt |
| `PushMessageDto` | Receiver's live hub arrival (`ReceiveMessage`) | Id, Content, Sender, SentAt |
| `StandardMessageDto` | Full conversation history (`GetConversation`) | Id, Content, Sender, ReceiverId, SentAt, Seen, SeenAt, IsEdited |
| `MessageEditedDto` | Edit push (`MessageEdited`) | MessageId, Content |

`SentMessageDto` has no `Seen`/`ReceiverId` — sender already knows both. `PushMessageDto` has no `Seen`/`SeenAt`/`IsEdited` — none of these exist yet at the instant a message is pushed. `MessageEditedDto` has no `EditedAt` — deferred until frontend work on the "edited" label begins.

## Hub events (server → client push)

| Event | Trigger | Payload | Target group |
|---|---|---|---|
| `ReceiveMessage` | `SendMessage` persists successfully | `PushMessageDto` | Receiver's group |
| `MessageSeen` | Conversation marked read (see below) | `{ SeenBy, SeenAt }` | Sender's group |
| `MessageEdited` | Message edit persists successfully | `MessageEditedDto` | Receiver's group |

No client-invokable hub methods (server ← client) currently exist. Typing indicators are the earmarked future use case for that pattern — ephemeral, no persistence, no authorization complexity, unlike message send/edit/read which all remain REST-driven for one consistent validation path.

## Read receipts

`Message.SeenAt` (nullable `DateTime`) added via migration — required to show "seen X ago" after a page reload, not just live (a boolean alone can't reconstruct *when*). Existing `Seen = true` rows backfilled with migration-time UTC now, since the actual historical read time was never recorded.

`Seen` kept as a redundant-but-harmless boolean (`ReadAt != null` would be equivalent) — cleaner query syntax at call sites, and it's updated atomically alongside `ReadAt` in the same `ExecuteUpdateAsync`, so no drift risk.

`MarkConversationAsReadAsync(readerId, otherUserId)`:
- Only marks messages and returns `true` if the **last message** in the conversation was sent by `otherUserId` and was unread.
- Guards against false-positive `MessageSeen` pushes when a reader reopens an already-read conversation.
- Bulk-updates all unread incoming messages (`IsRead`, `ReadAt`, `LastUpdatedAt`), not just the last one — UI only ever displays a seen-marker on the last message, but underlying read-state must stay accurate for all messages (unread counts, badges, etc.).

## Testing

Local HTML page + `@microsoft/signalr` JS client (via CDN, version matched to project's target framework), served through Live Server. Manual two-tab test (two different logged-in users) to verify push delivery.

Echo test script: on `ReceiveMessage`, reverse the message content and POST it back via REST to the original sender — exercises the full round trip (REST send → hub push → REST send-back → hub push again).

## Not implemented / explicitly deferred

- **Online/presence/active status** — not built at all.
- **Typing indicators** — earmarked as the correct future use case for a client-invokable hub method; not built yet.
- **`EditedAt` timestamp** — deferred to frontend work.