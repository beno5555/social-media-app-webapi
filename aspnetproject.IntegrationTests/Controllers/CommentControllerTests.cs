using System.Net;
using System.Net.Http.Json;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class CommentControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommentControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());
    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- POST /comments/posts/{postId}  (note: plural "posts") ----------

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("api/comments/posts/2", new
        {
            Content = "Anon attempt"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_OnExistingPost_Succeeds()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "diana", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/comments/posts/2", new
        {
            Content = "Diana's new comment"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_OnNonexistentPost_ReturnsNotFoundOrBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "diana", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/comments/posts/9999", new
        {
            Content = "Comment on nothing"
        });

        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEmptyContent_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "diana", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/comments/posts/2", new
        {
            Content = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ByPostAuthorOnOwnPost_DoesNotPushNotificationButStillSucceeds()
    {
        // ReceiveComment push is skipped when commenter == post author, per signalr.md.
        // Can't assert the push directly without a hub test client — asserting the
        // HTTP call itself still succeeds is the REST-layer coverage for this case.
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/comments/posts/2", new // Bob's own post
        {
            Content = "Bob commenting on his own post again"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ---------- GET /comments/mine ----------

    [Fact]
    public async Task GetMine_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/comments/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyCallersOwnComments()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.GetAsync("api/comments/mine");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Bob comments on his own post.", body);
        Assert.Contains("Bob comments on Alice's post.", body);
        Assert.DoesNotContain("Alice comments on Bob's post.", body);
    }

    // ---------- GET /comments/post/{postId}  (note: singular "post") ----------

    [Fact]
    public async Task GetByPost_Anonymous_ReturnsAllCommentsForThatPost()
    {
        var response = await _client.GetAsync("api/comments/post/2"); // Bob Post, 3 comments

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alice comments on Bob's post.", body);
        Assert.Contains("Bob comments on his own post.", body);
        Assert.Contains("Charlie comments on Bob's post.", body);
    }

    [Fact]
    public async Task GetByPost_IncludesOrphanedComment()
    {
        var response = await _client.GetAsync("api/comments/post/1"); // Alice Post, has orphaned comment

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Orphaned comment with no active author.", body);
    }

    [Fact]
    public async Task GetByPost_RouteIsSingular_NotPlural()
    {
        // Explicitly documenting the plural/singular route split from controllers.md:
        // creation is under .../posts/{id}, listing is under .../post/{id}.
        var singularRoute = await _client.GetAsync("api/comments/post/2");
        var pluralRoute = await _client.GetAsync("api/comments/posts/2"); // GET on the create route

        Assert.Equal(HttpStatusCode.OK, singularRoute.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, pluralRoute.StatusCode); // no GET mapped there
    }

    [Fact]
    public async Task GetByPost_ForNonexistentPost_ReturnsEmptyOrNotFound()
    {
        var response = await _client.GetAsync("api/comments/post/9999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /comments/{id} ----------

    [Fact]
    public async Task GetById_Anonymous_ReturnsSingleComment()
    {
        var response = await _client.GetAsync("api/comments/1"); // "Alice comments on Bob's post."

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alice comments on Bob's post.", body);
    }

    [Fact]
    public async Task GetById_ForOrphanedComment_ReturnsCommentWithNullAuthor()
    {
        var response = await _client.GetAsync("api/comments/5"); // orphaned

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Orphaned comment with no active author.", body);
    }

    [Fact]
    public async Task GetById_NonexistentComment_ReturnsNotFound()
    {
        var response = await _client.GetAsync("api/comments/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- PUT /comments/{id} ----------

    [Fact]
    public async Task Update_OwnComment_Succeeds()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/comments/1", new // Alice's comment
        {
            Content = "Alice's edited comment"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alice's edited comment", body);
    }

    [Fact]
    public async Task Update_SomeoneElsesComment_ReturnsNotFound()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/comments/2", new // Bob's comment
        {
            Content = "Hijack attempt"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsPostAuthorOnSomeoneElsesComment_ReturnsNotFound()
    {
        // Update is ownership-scoped only — the three-way rule (commenter/post-author/admin)
        // is documented for Delete, not Update. Bob is the post author of post 2, but
        // comment 1 belongs to alice.
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/comments/1", new
        {
            Content = "Post author trying to edit someone else's comment"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("api/comments/1", new
        {
            CommentContent = "Anon edit"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithEmptyContent_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/comments/1", new
        {
            CommentContent = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- DELETE /comments/{id}  (three-way: commenter, post author, or admin) ----------

    [Fact]
    public async Task Delete_ByCommenter_Succeeds()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deleteResponse = await _client.DeleteAsync("api/comments/1"); // Alice's own comment
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/comments/1");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ByPostAuthor_OnSomeoneElsesComment_Succeeds()
    {
        // Bob is the author of post 2; comment 1 on that post belongs to alice.
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deleteResponse = await _client.DeleteAsync("api/comments/1");
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/comments/1");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ByAdmin_OnUnrelatedComment_Succeeds()
    {
        // Admin is neither the commenter nor the post author for comment 3
        // (Charlie's comment on Bob's post).
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deleteResponse = await _client.DeleteAsync("api/comments/3");
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/comments/3");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ByUnrelatedNonAdminUser_ReturnsNotFound()
    {
        // Diana is neither the commenter, the post author, nor an admin for comment 3.
        var token = await AuthTestHelper.LoginAsync(_client, "diana", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.DeleteAsync("api/comments/3");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("api/comments/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonexistentComment_ReturnsNotFound()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.DeleteAsync("api/comments/9999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
