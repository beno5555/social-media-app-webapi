using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Dtos.Comments;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

// The moderation visibility rule on every read of a post or a comment, from each kind of caller.
// Content is hidden or flagged through the admin endpoint at the start of each test, so the seed stays all visible.
// Callers: sandro_beno (1) is the administrator, alice (2) is the author, charlie (4) is another user, and a guest has no token.
// bob (3) is another user too; he is used where his seeded like on alice's post, or his ownership of post 2, matters.
// Seed state used here (see TestDataSeeder):
//   post 1 is alice's, with comments 4 (bob) and 5 (orphaned), and one like by bob
//   post 2 is bob's, with comments 1 (alice), 2 (bob) and 3 (charlie)
[Collection("Database")]
public class ContentVisibilityTests : IAsyncLifetime
{
    private const string Administrator = "sandro_beno";
    private const string Author        = "alice";
    private const string OtherUser     = "charlie";
    private const string Liker         = "bob";
    private const string PostAuthor    = "bob";

    private const int AliceId              = 2;
    private const int AlicesPostId         = 1;
    private const int BobsPostId           = 2;
    private const int BobsPostCommentCount = 3;
    private const int AlicesCommentId      = 1;
    private const int BobsCommentId        = 2;
    private const int CharliesCommentId    = 3;

    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ContentVisibilityTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------- GET /posts/{id} ----------

    [Theory]
    [InlineData(OtherUser)]
    [InlineData(null)]
    public async Task GetPostById_HiddenPost_AsCallerWhoCannotReadIt_ReturnsNotFound(string? caller)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInOrStayGuestAsync(caller);

        var response = await _client.GetAsync($"api/posts/{AlicesPostId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ResponseMessages.PostNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Theory]
    [InlineData(Author)]
    [InlineData(Administrator)]
    public async Task GetPostById_HiddenPost_AsAuthorOrAdministrator_ReturnsPostMarkedHidden(string caller)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.GetAsync($"api/posts/{AlicesPostId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(response);
        Assert.True(post.IsHidden);
    }

    [Fact]
    public async Task GetPostById_VisiblePost_ReturnsPostNotMarkedHidden()
    {
        var response = await _client.GetAsync($"api/posts/{AlicesPostId}");

        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(response);
        Assert.False(post.IsHidden);
    }

    // ---------- GET /posts/feed ----------

    // The feed never holds the caller's own posts, so the author does not see it either.
    [Theory]
    [InlineData(OtherUser)]
    [InlineData(Author)]
    public async Task GetFeed_HiddenPost_AsOtherUserOrAuthor_LeavesItOut(string caller)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.GetAsync($"api/posts/feed?pageSize={Constants.MaxPageSize}");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(response);
        Assert.DoesNotContain(posts, post => post.Id == AlicesPostId);
    }

    // Feed candidates are every post the caller may read, and an administrator may read hidden posts.
    [Fact]
    public async Task GetFeed_HiddenPost_AsAdministrator_ReturnsItMarkedHidden()
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(Administrator);

        var response = await _client.GetAsync($"api/posts/feed?pageSize={Constants.MaxPageSize}");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(response);
        Assert.True(posts.Single(post => post.Id == AlicesPostId).IsHidden);
    }

    // charlie wrote the hidden comment, so he still counts it.
    [Theory]
    [InlineData(Author, BobsPostCommentCount - 1)]
    [InlineData(OtherUser, BobsPostCommentCount)]
    [InlineData(Administrator, BobsPostCommentCount)]
    public async Task GetFeed_PostWithHiddenComment_CountsOnlyCommentsTheCallerCanRead(string caller, int expectedCount)
    {
        await SetCommentStatusAsync(CharliesCommentId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.GetAsync($"api/posts/feed?pageSize={Constants.MaxPageSize}");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(response);
        Assert.Equal(expectedCount, posts.Single(post => post.Id == BobsPostId).CommentCount);
    }

    // ---------- GET /posts/user/{id} and /posts/mine ----------

    [Theory]
    [InlineData(OtherUser)]
    [InlineData(null)]
    public async Task GetPostsByUser_HiddenPost_AsCallerWhoCannotReadIt_LeavesItOut(string? caller)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInOrStayGuestAsync(caller);

        var response = await _client.GetAsync($"api/posts/user/{AliceId}");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(response);
        Assert.DoesNotContain(posts, post => post.Id == AlicesPostId);
    }

    [Theory]
    [InlineData(Author)]
    [InlineData(Administrator)]
    public async Task GetPostsByUser_HiddenPost_AsAuthorOrAdministrator_ReturnsItMarkedHidden(string caller)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.GetAsync($"api/posts/user/{AliceId}");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(response);
        Assert.True(posts.Single(post => post.Id == AlicesPostId).IsHidden);
    }

    [Fact]
    public async Task GetOwnPosts_HiddenPost_ReturnsItMarkedHidden()
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(Author);

        var response = await _client.GetAsync("api/posts/mine");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(response);
        Assert.True(posts.Single(post => post.Id == AlicesPostId).IsHidden);
    }

    // ---------- comments inside GET /posts/{id} ----------

    [Theory]
    [InlineData(OtherUser)]
    [InlineData(null)]
    public async Task GetPostById_WithHiddenComment_AsCallerWhoCannotReadIt_LeavesItOutOfCommentsAndCount(string? caller)
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInOrStayGuestAsync(caller);

        var response = await _client.GetAsync($"api/posts/{BobsPostId}");

        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(response);
        Assert.DoesNotContain(post.Comments, comment => comment.Id == AlicesCommentId);
        Assert.Equal(BobsPostCommentCount - 1, post.CommentCount);
    }

    [Theory]
    [InlineData(Author)]
    [InlineData(Administrator)]
    public async Task GetPostById_WithHiddenComment_AsAuthorOrAdministrator_ReturnsItMarkedHidden(string caller)
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.GetAsync($"api/posts/{BobsPostId}");

        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(response);
        Assert.True(post.Comments.Single(comment => comment.Id == AlicesCommentId).IsHidden);
        Assert.Equal(BobsPostCommentCount, post.CommentCount);
    }

    // ---------- GET /comments/{id} ----------

    [Theory]
    [InlineData(OtherUser)]
    [InlineData(null)]
    public async Task GetCommentById_HiddenComment_AsCallerWhoCannotReadIt_ReturnsNotFound(string? caller)
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInOrStayGuestAsync(caller);

        var response = await _client.GetAsync($"api/comments/{AlicesCommentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ResponseMessages.CommentNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Theory]
    [InlineData(Author)]
    [InlineData(Administrator)]
    public async Task GetCommentById_HiddenComment_AsAuthorOrAdministrator_ReturnsCommentMarkedHidden(string caller)
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.GetAsync($"api/comments/{AlicesCommentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var comment = await ResponseReader.ReadDataAsync<FullCommentDto>(response);
        Assert.True(comment.IsHidden);
    }

    // A visible comment on a hidden post would otherwise reveal the post.
    [Theory]
    [InlineData(OtherUser, HttpStatusCode.NotFound)]
    [InlineData(null, HttpStatusCode.NotFound)]
    [InlineData(Administrator, HttpStatusCode.OK)]
    public async Task GetCommentById_VisibleCommentOnHiddenPost_ReadsAsThePostDoes(string? caller, HttpStatusCode expectedStatus)
    {
        await SetPostStatusAsync(BobsPostId, "Hidden");
        await SignInOrStayGuestAsync(caller);

        var response = await _client.GetAsync($"api/comments/{BobsCommentId}");

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    // ---------- GET /comments/post/{id} and /comments/mine ----------

    [Theory]
    [InlineData(OtherUser)]
    [InlineData(null)]
    public async Task GetCommentsByPost_HiddenComment_AsCallerWhoCannotReadIt_LeavesItOut(string? caller)
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInOrStayGuestAsync(caller);

        var response = await _client.GetAsync($"api/comments/post/{BobsPostId}");

        var comments = await ResponseReader.ReadListAsync<StandardCommentDto>(response);
        Assert.DoesNotContain(comments, comment => comment.Id == AlicesCommentId);
        Assert.Equal(BobsPostCommentCount - 1, comments.Count);
    }

    [Theory]
    [InlineData(Author)]
    [InlineData(Administrator)]
    public async Task GetCommentsByPost_HiddenComment_AsAuthorOrAdministrator_ReturnsItMarkedHidden(string caller)
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.GetAsync($"api/comments/post/{BobsPostId}");

        var comments = await ResponseReader.ReadListAsync<StandardCommentDto>(response);
        Assert.True(comments.Single(comment => comment.Id == AlicesCommentId).IsHidden);
    }

    // The same answer as for a post that does not exist.
    [Theory]
    [InlineData(OtherUser)]
    [InlineData(null)]
    public async Task GetCommentsByPost_HiddenPost_AsCallerWhoCannotReadIt_ReadsAsMissingPost(string? caller)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInOrStayGuestAsync(caller);

        var response = await _client.GetAsync($"api/comments/post/{AlicesPostId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.PostNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Theory]
    [InlineData(Author)]
    [InlineData(Administrator)]
    public async Task GetCommentsByPost_HiddenPost_AsAuthorOrAdministrator_ReturnsComments(string caller)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.GetAsync($"api/comments/post/{AlicesPostId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOwnComments_HiddenComment_ReturnsItMarkedHidden()
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInAsync(Author);

        var response = await _client.GetAsync("api/comments/mine");

        var comments = await ResponseReader.ReadListAsync<StandardCommentDto>(response);
        Assert.True(comments.Single(comment => comment.Id == AlicesCommentId).IsHidden);
    }

    // ---------- likes on a hidden post ----------

    [Theory]
    [InlineData(OtherUser, HttpStatusCode.NotFound)]
    [InlineData(Author, HttpStatusCode.OK)]
    [InlineData(Administrator, HttpStatusCode.OK)]
    public async Task Like_HiddenPost_SucceedsOnlyForCallersWhoCanReadIt(string caller, HttpStatusCode expectedStatus)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.PostAsync($"api/posts/{AlicesPostId}/likes", null);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(Author)]
    [InlineData(Administrator)]
    public async Task Unlike_HiddenPost_AsAuthorOrAdministrator_Succeeds(string caller)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(caller);
        await _client.PostAsync($"api/posts/{AlicesPostId}/likes", null);

        var response = await _client.DeleteAsync($"api/posts/{AlicesPostId}/likes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // bob liked alice's post before it was hidden; he cannot read it now, so he cannot unlike it either.
    [Fact]
    public async Task Unlike_HiddenPost_AsLikerWhoCannotReadIt_ReturnsNotFound()
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(Liker);

        var response = await _client.DeleteAsync($"api/posts/{AlicesPostId}/likes");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ResponseMessages.PostNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Theory]
    [InlineData(OtherUser, HttpStatusCode.NotFound)]
    [InlineData(null, HttpStatusCode.NotFound)]
    [InlineData(Author, HttpStatusCode.OK)]
    [InlineData(Administrator, HttpStatusCode.OK)]
    public async Task GetLikers_HiddenPost_SucceedsOnlyForCallersWhoCanReadIt(string? caller, HttpStatusCode expectedStatus)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInOrStayGuestAsync(caller);

        var response = await _client.GetAsync($"api/posts/{AlicesPostId}/likes");

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    // ---------- commenting on a hidden post ----------

    // A refused comment gets the same answer as one on a post that does not exist.
    [Theory]
    [InlineData(OtherUser, HttpStatusCode.BadRequest)]
    [InlineData(Author, HttpStatusCode.Created)]
    [InlineData(Administrator, HttpStatusCode.Created)]
    public async Task CreateComment_OnHiddenPost_SucceedsOnlyForCallersWhoCanReadIt(string caller, HttpStatusCode expectedStatus)
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.PostAsJsonAsync($"api/comments/posts/{AlicesPostId}", new { Content = "A new comment." });

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    // ---------- the author edits and deletes hidden content ----------

    [Fact]
    public async Task UpdatePost_HiddenPost_AsAuthor_SucceedsAndStaysHidden()
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(Author);

        var response = await _client.PutAsJsonAsync($"api/posts/{AlicesPostId}", new { Title = "Edited title", Content = "Edited content." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await ResponseReader.ReadDataAsync<FullPostDisplayDto>(response)).IsHidden);
        await AssertNotFoundForOtherUserAsync($"api/posts/{AlicesPostId}");
    }

    [Fact]
    public async Task UpdatePost_HiddenPost_AsOtherUser_ReadsAsMissingPost()
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(OtherUser);

        var response = await _client.PutAsJsonAsync($"api/posts/{AlicesPostId}", new { Title = "Edited title", Content = "Edited content." });

        Assert.Equal(ResponseMessages.PostNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task DeletePost_HiddenPost_AsAuthor_Succeeds()
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(Author);

        var response = await _client.DeleteAsync($"api/posts/{AlicesPostId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EditComment_HiddenComment_AsAuthor_SucceedsAndStaysHidden()
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInAsync(Author);

        var response = await _client.PutAsJsonAsync($"api/comments/{AlicesCommentId}", new { Content = "Edited comment." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await ResponseReader.ReadDataAsync<FullCommentDto>(response)).IsHidden);
        await AssertNotFoundForOtherUserAsync($"api/comments/{AlicesCommentId}");
    }

    [Fact]
    public async Task DeleteComment_HiddenComment_AsAuthor_Succeeds()
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInAsync(Author);

        var response = await _client.DeleteAsync($"api/comments/{AlicesCommentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeletePost_HiddenPost_AsOtherUser_ReadsAsMissingPost()
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(OtherUser);

        var response = await _client.DeleteAsync($"api/posts/{AlicesPostId}");

        Assert.Equal(ResponseMessages.PostNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task DeletePost_HiddenPost_AsAdministrator_Succeeds()
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SignInAsync(Administrator);

        var response = await _client.DeleteAsync($"api/posts/{AlicesPostId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // An administrator can read the hidden comment but, like anyone but its author, cannot edit it.
    [Theory]
    [InlineData(OtherUser, ResponseMessages.CommentNotFound)]
    [InlineData(Administrator, ResponseMessages.OnlyCommenterCanEditMessage)]
    public async Task EditComment_HiddenComment_AsSomeoneElse_IsRefused(string caller, string expectedMessage)
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.PutAsJsonAsync($"api/comments/{AlicesCommentId}", new { Content = "Edited comment." });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expectedMessage, await ResponseReader.ReadMessageAsync(response));
    }

    // bob owns the post, which normally lets him delete any comment on it, but he cannot read alice's hidden one.
    [Theory]
    [InlineData(OtherUser)]
    [InlineData(PostAuthor)]
    public async Task DeleteComment_HiddenComment_AsCallerWhoCannotReadIt_ReadsAsMissingComment(string caller)
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInAsync(caller);

        var response = await _client.DeleteAsync($"api/comments/{AlicesCommentId}");

        Assert.Equal(ResponseMessages.CommentNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task DeleteComment_HiddenComment_AsAdministrator_Succeeds()
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SignInAsync(Administrator);

        var response = await _client.DeleteAsync($"api/comments/{AlicesCommentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // alice's own comment sits on bob's post; once the post is hidden, the comment goes with it.
    [Fact]
    public async Task GetOwnComments_OwnCommentOnAnotherUsersHiddenPost_LeavesItOut()
    {
        await SetPostStatusAsync(BobsPostId, "Hidden");
        await SignInAsync(Author);

        var response = await _client.GetAsync("api/comments/mine");

        var comments = await ResponseReader.ReadListAsync<StandardCommentDto>(response);
        Assert.DoesNotContain(comments, comment => comment.Id == AlicesCommentId);
    }

    // ---------- new content ----------

    [Fact]
    public async Task UploadPost_NewPost_StartsVisibleToOtherUsers()
    {
        await SignInAsync(Author);
        var created = await _client.PostAsync("api/posts", PostForm.Create("New post", "Fresh content."));
        var postId  = (await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(created)).Id;
        await SignInAsync(OtherUser);

        var response = await _client.GetAsync($"api/posts/{postId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await ResponseReader.ReadDataAsync<FullPostDisplayDto>(response)).IsHidden);
    }

    [Fact]
    public async Task CreateComment_NewComment_StartsVisibleToOtherUsers()
    {
        await SignInAsync(Author);
        var created   = await _client.PostAsJsonAsync($"api/comments/posts/{BobsPostId}", new { Content = "A new comment." });
        var commentId = (await ResponseReader.ReadDataAsync<FullCommentDto>(created)).Id;
        await SignInAsync(OtherUser);

        var response = await _client.GetAsync($"api/comments/{commentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await ResponseReader.ReadDataAsync<FullCommentDto>(response)).IsHidden);
    }

    // ---------- flagged content ----------

    // Flagged reads as visible, and no ordinary shape names the status.
    [Theory]
    [InlineData(OtherUser)]
    [InlineData(null)]
    public async Task GetPostById_FlaggedPostAndComment_ReadsAsVisible(string? caller)
    {
        await SetPostStatusAsync(BobsPostId, "Flagged");
        await SetCommentStatusAsync(AlicesCommentId, "Flagged");
        await SignInOrStayGuestAsync(caller);

        var response = await _client.GetAsync($"api/posts/{BobsPostId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Flagged", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(response);
        Assert.False(post.IsHidden);
        Assert.False(post.Comments.Single(comment => comment.Id == AlicesCommentId).IsHidden);
        Assert.Equal(BobsPostCommentCount, post.CommentCount);
    }

    // ---------- restoring hidden content ----------

    [Fact]
    public async Task SetPostStatus_HiddenPostBackToVisible_ReturnsItToEveryRead()
    {
        await SetPostStatusAsync(AlicesPostId, "Hidden");
        await SetPostStatusAsync(AlicesPostId, "Visible");
        await SignInOrStayGuestAsync(null);

        var byId   = await _client.GetAsync($"api/posts/{AlicesPostId}");
        var byUser = await _client.GetAsync($"api/posts/user/{AliceId}");

        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.False((await ResponseReader.ReadDataAsync<FullPostDisplayDto>(byId)).IsHidden);
        Assert.Contains(await ResponseReader.ReadListAsync<StandardPostDisplayDto>(byUser), post => post.Id == AlicesPostId);
    }

    [Fact]
    public async Task SetCommentStatus_HiddenCommentBackToVisible_ReturnsItToEveryRead()
    {
        await SetCommentStatusAsync(AlicesCommentId, "Hidden");
        await SetCommentStatusAsync(AlicesCommentId, "Visible");
        await SignInOrStayGuestAsync(null);

        var byId   = await _client.GetAsync($"api/comments/{AlicesCommentId}");
        var byPost = await _client.GetAsync($"api/comments/post/{BobsPostId}");

        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(BobsPostCommentCount, (await ResponseReader.ReadListAsync<StandardCommentDto>(byPost)).Count);
    }

    // ---------- helpers ----------

    private async Task AssertNotFoundForOtherUserAsync(string url)
    {
        await SignInAsync(OtherUser);
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SignInAsync(string username)
    {
        var token = await AuthTestHelper.LoginAsync(_client, username, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
    }

    // A null caller is a guest: the client sends no token.
    private async Task SignInOrStayGuestAsync(string? caller)
    {
        _client.DefaultRequestHeaders.Authorization = null;

        if (caller is not null)
        {
            await SignInAsync(caller);
        }
    }

    // Signs in as the administrator to change the status; the test then signs in as the caller it reads as.
    private async Task SetPostStatusAsync(int postId, string status)
    {
        await SignInAsync(Administrator);
        var response = await _client.PutAsJsonAsync($"api/admin/posts/{postId}/moderation", new { Status = status });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task SetCommentStatusAsync(int commentId, string status)
    {
        await SignInAsync(Administrator);
        var response = await _client.PutAsJsonAsync($"api/admin/comments/{commentId}/moderation", new { Status = status });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
