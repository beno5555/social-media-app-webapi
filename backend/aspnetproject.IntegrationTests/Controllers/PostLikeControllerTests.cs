using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

// Seeded likes, newest first:
//   5: diana (5)            likes post 9 (charlie's, a feed ranking post)
//   4: bob (3)              likes post 1 (alice's)
//   3: erin_deactivated (6) likes post 2 (bob's)
//   2: charlie (4)          likes post 2
//   1: alice (2)            likes post 2
// Posts 3 (charlie's), 4 (erin's) and 5 (frank's) have no likes.
[Collection("Database")]
public class PostLikeControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture             _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    public PostLikeControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory   = new CustomWebApplicationFactory();
        _client    = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------- POST /posts/{postId}/likes ----------

    [Fact]
    public async Task Like_PostNotYetLiked_ReturnsOk()
    {
        await LoginAsync("alice");

        var response = await _client.PostAsync("api/posts/3/likes", null); // charlie's post

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ResponseMessages.PostLiked, await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Like_AlreadyLikedPost_ReturnsOkAndKeepsOneLike()
    {
        await LoginAsync("alice");

        var response = await _client.PostAsync("api/posts/2/likes", null); // alice already likes bob's post
        var likers   = await GetLikersAsync("api/posts/2/likes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ResponseMessages.PostLiked, await ReadMessageAsync(response));
        Assert.Equal([6, 4, 2], likers.Select(liker => liker.Id));
    }

    [Fact]
    public async Task Like_SamePostTwice_ReturnsOkAndCountsOneLike()
    {
        await LoginAsync("diana");

        var first  = await _client.PostAsync("api/posts/3/likes", null);
        var second = await _client.PostAsync("api/posts/3/likes", null);
        var likers = await GetLikersAsync("api/posts/3/likes");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var liker = Assert.Single(likers);
        Assert.Equal(5, liker.Id); // diana
    }

    [Fact]
    public async Task Like_NonexistentPost_ReturnsNotFound()
    {
        await LoginAsync("alice");

        var response = await _client.PostAsync("api/posts/9999/likes", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ResponseMessages.PostNotFound, await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Like_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("api/posts/3/likes", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- DELETE /posts/{postId}/likes ----------

    [Fact]
    public async Task Unlike_PostLiked_ReturnsOkAndRemovesTheLike()
    {
        await LoginAsync("alice");

        var response = await _client.DeleteAsync("api/posts/2/likes");
        var likers   = await GetLikersAsync("api/posts/2/likes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ResponseMessages.PostUnliked, await ReadMessageAsync(response));
        Assert.DoesNotContain(likers, liker => liker.Id == 2);
    }

    [Fact]
    public async Task Unlike_PostNotLiked_ReturnsNotFound()
    {
        await LoginAsync("diana");

        var response = await _client.DeleteAsync("api/posts/2/likes");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ResponseMessages.PostNotLiked, await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Unlike_NonexistentPost_ReturnsNotFound()
    {
        await LoginAsync("alice");

        var response = await _client.DeleteAsync("api/posts/9999/likes");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ResponseMessages.PostNotFound, await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Unlike_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("api/posts/2/likes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unlike_ThenLikeAgain_ReturnsOk()
    {
        await LoginAsync("alice");

        await _client.DeleteAsync("api/posts/2/likes");
        var response = await _client.PostAsync("api/posts/2/likes", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- GET /posts/{postId}/likes ----------

    [Fact]
    public async Task GetLikers_AsGuest_ReturnsLikersNewestFirst()
    {
        var response = await _client.GetAsync("api/posts/2/likes");
        var body     = await response.Content.ReadFromJsonAsync<ListResponse<MinimalUserDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ResponseMessages.PostLikesRetrieved, body!.Message);
        Assert.Equal([6, 4, 2], body.Data.Select(liker => liker.Id));
        Assert.Equal(
            [ResponseMessages.AccountDeactivatedUsername, "charlie", "alice"],
            body.Data.Select(liker => liker.Username));
    }

    [Fact]
    public async Task GetLikers_WithPaging_ReturnsRequestedPage()
    {
        var likers = await GetLikersAsync("api/posts/2/likes?pageNumber=2&pageSize=2");

        var liker = Assert.Single(likers);
        Assert.Equal(2, liker.Id);
        Assert.Equal("alice", liker.Username);
    }

    [Fact]
    public async Task GetLikers_PostWithNoLikes_ReturnsEmptyList()
    {
        var likers = await GetLikersAsync("api/posts/3/likes");

        Assert.Empty(likers);
    }

    [Fact]
    public async Task GetLikers_AfterLike_ShowsTheNewLikerFirst()
    {
        await LoginAsync("diana");
        await _client.PostAsync("api/posts/2/likes", null);

        var likers = await GetLikersAsync("api/posts/2/likes");

        Assert.Equal([5, 6, 4, 2], likers.Select(liker => liker.Id));
        Assert.Equal("diana", likers[0].Username);
    }

    [Fact]
    public async Task GetLikers_NonexistentPost_ReturnsNotFound()
    {
        var response = await _client.GetAsync("api/posts/9999/likes");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ResponseMessages.PostNotFound, await ReadMessageAsync(response));
    }

    // ---------- helpers ----------

    private async Task<List<MinimalUserDto>> GetLikersAsync(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ListResponse<MinimalUserDto>>();
        return body!.Data;
    }

    private async Task LoginAsync(string username)
    {
        var token = await AuthTestHelper.LoginAsync(_client, username, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        return body!.Message;
    }
}
