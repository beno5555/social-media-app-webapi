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
    public Task DisposeAsync()    => Task.CompletedTask;
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

## 6. Pin suspected bugs with a test, then report them

```csharp
[Fact]
public async Task EditMessage_WithinEditWindow_Succeeds()   // currently fails: comparison looks inverted
```

## 7. Report honestly

Report as: "Ran 110 tests: 108 passed, 2 failed (`EditMessage_WithinEditWindow_Succeeds`, ...)". Never write "all tests pass" without having run them.
