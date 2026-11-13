# Testing

## 1. Integration tests per endpoint

Test through the real HTTP pipeline (`WebApplicationFactory`). For each endpoint, cover at least:

| Case | Example |
|---|---|
| Unauthenticated | `GetFeed_WithoutToken_ReturnsUnauthorized` |
| Happy path | `GetMine_ReturnsOnlyCallersOwnPosts` |
| Not found | `GetById_ForNonexistentPost_ReturnsNotFound` |
| Ownership / permission | `UpdatePost_ByNonOwner_ReturnsNotFound` |
| Edge case | `GetById_ForSoftDeletedUsersPost_StillReturnsPost` |

## 2. Isolated database, reset and reseeded per test

Never run tests against the dev database.

```csharp
[Collection("Database")]
public class PostControllerTests : IAsyncLifetime
{
    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());

    // Disposing the host releases its settings-file watchers; each one holds an inotify instance, and Linux caps them per user.
    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}

public async Task ResetAndSeedAsync(DbContextOptions<ApplicationDbContext> options)
{
    await _respawner.ResetAsync(_connection);            // keeps __EFMigrationsHistory
    await using var db = new ApplicationDbContext(options);
    await TestDataSeeder.SeedAsync(db);                  // deterministic ids and users
}
```

## 3. Name tests `Method_Condition_ExpectedResult`

## 4. Group tests by endpoint and describe the seed state they rely on

```csharp
// ---------- GET /posts/feed ----------
// alice (2) is friends (accepted) with bob (3) only.
// pending/declined relationships (diana, charlie) should not surface in feed.
```

Give magic ids a comment:
```csharp
var response = await _client.GetAsync("api/posts/2"); // Bob Post, has 3 comments
```

## 5. Use the arrange / act / assert layout with blank lines

```csharp
var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
AuthTestHelper.AttachToken(_client, token);

var response = await _client.GetAsync("api/posts/mine");

Assert.Equal(HttpStatusCode.OK, response.StatusCode);
```

## 6. Seed time-dependent cases with fixed past dates

A rule that depends on age (an edit window, a cooldown, a retention period) needs seed rows on both sides of the boundary. Use `DateTime.UtcNow` for "fresh" and a fixed UTC date far in the past for "expired", never `UtcNow` minus a window, which drifts when the constant changes:

```csharp
// A fixed time in the past, so it is always older than Constants.EditMessageWindow.
CreatedAt = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc)
```

## 7. Test real-time pushes with a real client

Connect `Helpers/HubTestClient` as each seeded user involved, act through HTTP, then assert on what each one received. Never mock or replace `IHubContext`. For every push, cover:

- the target receives it, with the expected payload (`WaitForEventAsync<T>`);
- every other connected user, including the actor, receives nothing (`AssertNoEventAsync`);
- a failed request pushes nothing.

```csharp
await using var alice = await HubTestClient.ConnectAsync(_factory, "alice");
await using var bob   = await HubTestClient.ConnectAsync(_factory, "bob");
// ... alice edits message 3 through HTTP ...
var pushed = await bob.WaitForEventAsync<MessageEditedDto>(HubEvents.MessageEdited);

await alice.AssertNoEventAsync(HubEvents.MessageEdited);
```

A new server event must also be added to `HubEvents.All`, or the client does not record it.

## 8. Pin suspected bugs with a test, then report them

Write the test for the intended behavior and let it fail; say so in a comment that points at the doc note (see `comments-and-docs.md` §5). Remove the comment when the bug is fixed.

```csharp
[Fact]
public async Task DoThing_UnderCondition_ExpectedResult()   // currently fails: see "Possible bug" in docs/backend/<area>.md
```

## 9. Report honestly

Report the counts, the failing names and the run time: "Ran 159 tests: 157 passed, 2 failed (`DoThing_UnderCondition_ExpectedResult`, ...). Duration: 18 s". Never write "all tests pass" without having run them.
