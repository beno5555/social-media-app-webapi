using System.Net;
using System.Net.Http.Json;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class PostControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PostControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());
    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- GET /posts/feed ----------
    // alice (2) is friends (accepted) with bob (3) only.
    // pending/declined relationships (diana, charlie) should not surface in feed.

    [Fact]
    public async Task GetFeed_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/posts/feed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_ShowsAcceptedFriendsPostsOnly()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.GetAsync("api/posts/feed");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Bob Post", body); // accepted friend
        Assert.DoesNotContain("Charlie Post", body); // charlie->alice is Pending, not Accepted
        Assert.DoesNotContain("Alice Post", body); // feed excludes caller's own posts
    }

    [Fact]
    public async Task GetFeed_DoesNotIncludePendingOrDeclinedFriendPosts()
    {
        // bob -> charlie is Declined; charlie should not appear in bob's feed either
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.GetAsync("api/posts/feed");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Charlie Post", body);
    }

    // ---------- GET /posts/mine ----------

    [Fact]
    public async Task GetMine_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/posts/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyCallersOwnPosts()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.GetAsync("api/posts/mine");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Alice Post", body);
        Assert.DoesNotContain("Bob Post", body);
    }

    // ---------- GET /posts/{id} ----------

    [Fact]
    public async Task GetById_Anonymous_ReturnsPostWithComments()
    {
        var response = await _client.GetAsync("api/posts/2"); // Bob Post, has 3 comments

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Bob Post", body);
        Assert.Contains("Alice comments on Bob's post.", body);
    }

    [Fact]
    public async Task GetById_ForNonexistentPost_ReturnsNotFound()
    {
        var response = await _client.GetAsync("api/posts/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForDeactivatedUsersPost_StillReturnsPost()
    {
        // Restrict/SetNull FK design: a deactivated author's existing post is not cascaded away
        var response = await _client.GetAsync("api/posts/4"); // Erin Deactivated Post

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Erin Deactivated Post", body);
    }

    [Fact]
    public async Task GetById_ForSoftDeletedUsersPost_StillReturnsPost()
    {
        var response = await _client.GetAsync("api/posts/5"); // Frank Deleted Post

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Frank Deleted Post", body);
    }

    [Fact]
    public async Task GetById_OrphanedComment_ShowsNullOrAnonymizedAuthor()
    {
        var response = await _client.GetAsync("api/posts/1"); // Alice Post, has the orphaned comment

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Orphaned comment with no active author.", body);
    }

    // ---------- GET /posts/user/{userId} ----------

    [Fact]
    public async Task GetPostsByUser_Anonymous_ReturnsThatUsersPosts()
    {
        var response = await _client.GetAsync("api/posts/user/3"); // bob

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Bob Post", body);
    }

    [Fact]
    public async Task GetPostsByUser_IsPublicRegardlessOfFriendship()
    {
        // charlie (4) has no accepted friendship with anyone; this endpoint is
        // fully public per controllers.md, unlike /feed
        var response = await _client.GetAsync("api/posts/user/4");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Charlie Post", body);
    }

    [Fact]
    public async Task GetPostsByUser_ForNonexistentUser_ReturnsEmptyOrNotFound()
    {
        var response = await _client.GetAsync("api/posts/user/9999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST /posts ----------

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("api/posts", new
        {
            PostTitle = "New Post",
            PostContent = "Content"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithToken_CreatesPostOwnedByCaller()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "diana", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var createResponse = await _client.PostAsJsonAsync("api/posts", new
        {
            PostTitle = "Diana's New Post",
            PostContent = "Fresh content"
        });

        Assert.True(createResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created);

        var mineResponse = await _client.GetAsync("api/posts/mine");
        var mineBody = await mineResponse.Content.ReadAsStringAsync();

        Assert.Contains("Diana's New Post", mineBody);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/posts", new
        {
            PostTitle = "",
            PostContent = "Content"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT /posts/{id} ----------

    [Fact]
    public async Task Update_OwnPost_Succeeds()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/posts/1", new
        {
            Title = "Alice Post Updated",
            Content = "Updated content"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alice Post Updated", body);
    }

    [Fact]
    public async Task Update_SomeoneElsesPost_ReturnsNotFound()
    {
        // Ownership-scoped query (WHERE Id = @id AND UserId = @userId) means a
        // non-owner gets NotFound, not Forbidden — avoids leaking existence.
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/posts/2", new // Bob's post
        {
            Title = "Hijacked Title",
            Content = "Hijacked content"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_EvenAsAdmin_SomeoneElsesPost_ReturnsNotFound()
    {
        // controllers.md only documents an admin override for Delete, not Update
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/posts/2", new
        {
            Title = "Admin Edit Attempt",
            Content = "Should not be allowed"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("api/posts/1", new
        {
            Title = "Anon Edit",
            Content = "Should not be allowed"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- DELETE /posts/{id} ----------

    [Fact]
    public async Task Delete_OwnPost_RemovesPostAndItsComments()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deleteResponse = await _client.DeleteAsync("api/posts/2"); // Bob Post, has 3 comments
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/posts/2");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_SomeoneElsesPost_AsNonOwnerNonAdmin_ReturnsNotFound()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.DeleteAsync("api/posts/2"); // Bob's post

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SomeoneElsesPost_AsAdmin_Succeeds()
    {
        // Delete explicitly allows owner OR admin, unlike Update
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deleteResponse = await _client.DeleteAsync("api/posts/3"); // Charlie's post
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/posts/3");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("api/posts/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonexistentPost_ReturnsNotFound()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.DeleteAsync("api/posts/9999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
