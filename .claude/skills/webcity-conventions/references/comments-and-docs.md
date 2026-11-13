# Comments and Docs

## 1. Explain why, not what

Avoid:
```csharp
// reverse the list
messages.Reverse();
```

Prefer:
```csharp
messages.Reverse(); // repository fetches newest-first for paging; display needs oldest-first
```

## 2. Comment these kinds of things

| Kind | Example |
|---|---|
| Sentinel value | `public DateTime? LastOnlineAt { get; set; } // null = online` |
| Deliberate trade-off | `// rare TOCTOU race fails safely as 409; Serializable isolation not worth the cost` |
| Cross-layer obligation | `// service must delete friendships before deleting the user (NoAction FK)` |
| Intentional oddity | `// singular "post" kept: clients depend on this route` |

## 3. Doc comments only on non-obvious public members

```csharp
/// <summary>
/// Fetches the latest posts of the users whose ids are in <paramref name="friendIds"/>.
/// </summary>
public async Task<List<Post>> GetFeedAsync(List<int> friendIds, int? pageNumber, int? pageSize)
```

Skip them on obvious members such as `GetByIdAsync` or simple properties.

## 4. TODOs must name a concrete action

Avoid:
```csharp
// TODO improve this
```

Prefer:
```csharp
// TODO count failed logins and email a warning after every 3 invalid attempts
```

## 5. Keep project docs in sync

When you change behavior described in project docs (README, area docs), update them in the same change. Record suspected bugs in the docs as clearly labelled notes rather than fixing them silently:

```markdown
**Possible bug: <what looks wrong>.** `<the code in question>` does <what it actually does>, which
contradicts <the name, message or doc that states the intent>. Confirm intended behavior before relying on either reading.
```

Delete the note, everywhere it appears, in the same change that fixes the bug or confirms the behavior is intended.
