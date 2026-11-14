using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
using aspnetproject.Infrastructure.Dtos.Moderation;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class AdminControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());
    // Disposing the host releases its settings-file watchers; each one holds an inotify instance, and Linux caps them per user.
    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task SignInAsync(string username)
    {
        var token = await AuthTestHelper.LoginAsync(_client, username, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
    }

    // Page size 100 holds every seeded row plus anything a test writes, unless the test pages on purpose.
    private async Task<List<LogDisplayDto>> GetLogsAsync(string filters = "", string paging = "pageSize=100")
    {
        var response = await _client.GetAsync($"api/admin/logs?{paging}&{filters}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ListResponse<LogDisplayDto>>();
        return body!.Data;
    }

    private async Task<List<AdminUserDto>> GetAccountsAsync(string query = "")
    {
        var response = await _client.GetAsync($"api/admin/users?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ListResponse<AdminUserDto>>();
        return body!.Data;
    }

    // ---------- GET /admin/logs ----------
    // sandro_beno (1) is the only administrator; alice (2) is an ordinary user.
    // Seeded logs, one day apart from 2026-09-01 (id 1) to 2026-09-05 (id 5):
    //   1: alice,  authorized, succeeded, PostService.UploadPost,                         Post 1
    //   2: bob,    authorized, failed,    PostService.UpdatePostAsync,                    Post 2
    //   3: guest,  anonymous,  failed,    AuthService.LoginAsync,                         User
    //   4: alice,  authorized, succeeded, CommentService.UploadCommentAsync,              Comment 1
    //   5: system, anonymous,  succeeded, SoftDeletedUsersCleanupService.ExecuteCycleAsync, User
    // Rows written while a test runs are newer than all of these.

    [Fact]
    public async Task GetLogs_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/admin/logs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_AsOrdinaryUser_ReturnsForbidden()
    {
        await SignInAsync("alice");

        var response = await _client.GetAsync("api/admin/logs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_AsAdministrator_ReturnsRowsNewestFirst()
    {
        await SignInAsync("sandro_beno");

        var logs = await GetLogsAsync();

        var seededIds = logs.Select(log => log.Id).Where(id => id <= 5).ToList();
        Assert.Equal([5, 4, 3, 2, 1], seededIds);
        Assert.Equal(logs.OrderByDescending(log => log.CreatedAt).Select(log => log.Id), logs.Select(log => log.Id));
    }

    [Fact]
    public async Task GetLogs_SecondPage_ReturnsTheNextOlderRows()
    {
        await SignInAsync("sandro_beno");
        var allLogs = await GetLogsAsync();

        var firstPage  = await GetLogsAsync(paging: "pageNumber=1&pageSize=2");
        var secondPage = await GetLogsAsync(paging: "pageNumber=2&pageSize=2");

        Assert.Equal(allLogs.Take(2).Select(log => log.Id), firstPage.Select(log => log.Id));
        Assert.Equal(allLogs.Skip(2).Take(2).Select(log => log.Id), secondPage.Select(log => log.Id));
    }

    [Theory]
    [InlineData("userId=2", new[] { 4, 1 })]
    [InlineData("authorizedRequest=false", new[] { 5, 3 })]
    [InlineData("succeeded=false", new[] { 3, 2 })]
    [InlineData("entityName=Post", new[] { 2, 1 })]
    [InlineData("entityId=1", new[] { 4, 1 })]
    [InlineData("action=PostService", new[] { 2, 1 })]
    [InlineData("from=2026-09-02T00:00:00Z&to=2026-09-04T23:59:59Z", new[] { 4, 3, 2 })]
    [InlineData("entityName=Post&succeeded=false", new[] { 2 })]
    [InlineData("userId=2&entityName=Comment&entityId=1", new[] { 4 })]
    public async Task GetLogs_WithFilters_ReturnsOnlyMatchingRows(string filters, int[] expectedSeededIds)
    {
        await SignInAsync("sandro_beno");

        var logs = await GetLogsAsync(filters);

        // Rows written during the test are newer than the seed; only the seeded ones have a known answer.
        var seededIds = logs.Select(log => log.Id).Where(id => id <= 5).ToArray();
        Assert.Equal(expectedSeededIds, seededIds);
    }

    [Fact]
    public async Task GetLogs_WithDateFilter_LeavesOutRowsOutsideTheRange()
    {
        await SignInAsync("sandro_beno");

        var logs = await GetLogsAsync("to=2026-09-01T23:59:59Z");

        Assert.Equal([1], logs.Select(log => log.Id));
    }

    [Fact]
    public async Task GetLogs_WithStartAfterEnd_ReturnsValidationProblem()
    {
        await SignInAsync("sandro_beno");

        var response = await _client.GetAsync("api/admin/logs?from=2026-09-05T00:00:00Z&to=2026-09-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errors\"", body); // the framework's validation ProblemDetails, not the envelope
    }

    // ---------- GET /admin/users ----------
    // Seeded accounts: sandro_beno (1, administrator), alice (2), bob (3), charlie (4), diana (5) are active;
    // erin_deactivated (6) was deactivated 2026-09-06 11:50; frank_deleted (7) was soft-deleted 2026-08-12 12:00.

    [Fact]
    public async Task GetAccounts_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccounts_AsOrdinaryUser_ReturnsForbidden()
    {
        await SignInAsync("alice");

        var response = await _client.GetAsync("api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAccounts_AsAdministrator_ReturnsAccountsInEveryStateWithTheirState()
    {
        await SignInAsync("sandro_beno");

        var accounts = await GetAccountsAsync();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7], accounts.Select(account => account.Id));

        var admin = accounts.Single(account => account.Id == 1);
        Assert.Equal("sandro_beno", admin.Username);
        Assert.True(admin.IsAdministrator);
        Assert.Equal(AccountState.Active, admin.State);
        Assert.Null(admin.DeactivatedAt);
        Assert.Null(admin.DeletedAt);

        var alice = accounts.Single(account => account.Id == 2);
        Assert.False(alice.IsAdministrator);

        var erin = accounts.Single(account => account.Id == 6);
        Assert.Equal(AccountState.Deactivated, erin.State);
        Assert.Equal(new DateTime(2026, 9, 6, 11, 50, 0), erin.DeactivatedAt);

        var frank = accounts.Single(account => account.Id == 7);
        Assert.Equal(AccountState.SoftDeleted, frank.State);
        Assert.Equal(new DateTime(2026, 8, 12, 12, 0, 0), frank.DeletedAt);
    }

    [Fact]
    public async Task GetAccounts_ReturnsStateAsText()
    {
        await SignInAsync("sandro_beno");

        var response = await _client.GetAsync("api/admin/users?state=Deactivated");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"state\":\"Deactivated\"", body);
    }

    [Theory]
    [InlineData("state=Active", new[] { 1, 2, 3, 4, 5 })]
    [InlineData("state=Deactivated", new[] { 6 })]
    [InlineData("state=SoftDeleted", new[] { 7 })]
    [InlineData("username=frank", new[] { 7 })]
    [InlineData("username=a&state=Active", new[] { 1, 2, 4, 5 })]
    public async Task GetAccounts_WithFilters_ReturnsOnlyMatchingAccounts(string filters, int[] expectedIds)
    {
        await SignInAsync("sandro_beno");

        var accounts = await GetAccountsAsync(filters);

        Assert.Equal(expectedIds, accounts.Select(account => account.Id));
    }

    [Fact]
    public async Task GetAccounts_SecondPage_ReturnsTheNextAccounts()
    {
        await SignInAsync("sandro_beno");

        var accounts = await GetAccountsAsync("pageNumber=2&pageSize=3");

        Assert.Equal([4, 5, 6], accounts.Select(account => account.Id));
    }

    // ---------- PUT /admin/posts/{id}/moderation ----------
    // Seeded posts start visible. Post 1 is alice's; post 999 does not exist.

    private Task<HttpResponseMessage> SetPostStatusAsync(int postId, string status)
    {
        return _client.PutAsJsonAsync($"api/admin/posts/{postId}/moderation", new { Status = status });
    }

    private Task<HttpResponseMessage> SetCommentStatusAsync(int commentId, string status)
    {
        return _client.PutAsJsonAsync($"api/admin/comments/{commentId}/moderation", new { Status = status });
    }

    [Fact]
    public async Task SetPostStatus_WithoutToken_ReturnsUnauthorized()
    {
        var response = await SetPostStatusAsync(1, "Hidden");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetPostStatus_AsOrdinaryUser_ReturnsForbidden()
    {
        await SignInAsync("alice");

        var response = await SetPostStatusAsync(1, "Hidden");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Hidden")]
    [InlineData("Flagged")]
    [InlineData("Visible")]
    [InlineData("HIDDEN")]
    public async Task SetPostStatus_AsAdministrator_ReturnsOk(string status)
    {
        await SignInAsync("sandro_beno");

        var response = await SetPostStatusAsync(1, status);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetPostStatus_ForNonexistentPost_ReturnsNotFound()
    {
        await SignInAsync("sandro_beno");

        var response = await SetPostStatusAsync(999, "Hidden");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ResponseMessages.PostNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Theory]
    [InlineData("Deleted")]
    [InlineData("0")]
    [InlineData("3")]
    public async Task SetPostStatus_WithUnknownStatus_ReturnsBadRequest(string status)
    {
        await SignInAsync("sandro_beno");

        var response = await SetPostStatusAsync(1, status);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Only names are accepted: 3 is Hidden's underlying value, and must not set it.
    [Fact]
    public async Task SetPostStatus_WithStatusAsNumber_ReturnsBadRequest()
    {
        await SignInAsync("sandro_beno");

        var response = await _client.PutAsJsonAsync("api/admin/posts/1/moderation", new { Status = 3 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetPostStatus_WhenStatusChanges_WritesLogRow()
    {
        await SignInAsync("sandro_beno");

        await SetPostStatusAsync(1, "Hidden");

        var logs = await GetLogsAsync("action=ModerationService.SetPostStatusAsync&entityId=1&succeeded=true");
        var log  = Assert.Single(logs);
        Assert.Equal("Post", log.EntityName);
        Assert.Equal(1, log.UserId);
    }

    // ---------- PUT /admin/comments/{id}/moderation ----------
    // Seeded comments start visible. Comment 1 is alice's; comment 999 does not exist.

    [Fact]
    public async Task SetCommentStatus_WithoutToken_ReturnsUnauthorized()
    {
        var response = await SetCommentStatusAsync(1, "Hidden");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetCommentStatus_AsOrdinaryUser_ReturnsForbidden()
    {
        await SignInAsync("alice");

        var response = await SetCommentStatusAsync(1, "Hidden");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Hidden")]
    [InlineData("Flagged")]
    [InlineData("Visible")]
    public async Task SetCommentStatus_AsAdministrator_ReturnsOk(string status)
    {
        await SignInAsync("sandro_beno");

        var response = await SetCommentStatusAsync(1, status);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetCommentStatus_ForNonexistentComment_ReturnsNotFound()
    {
        await SignInAsync("sandro_beno");

        var response = await SetCommentStatusAsync(999, "Hidden");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ResponseMessages.CommentNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task SetCommentStatus_WhenStatusChanges_WritesLogRow()
    {
        await SignInAsync("sandro_beno");

        await SetCommentStatusAsync(1, "Hidden");

        var logs = await GetLogsAsync("action=ModerationService.SetCommentStatusAsync&entityId=1&succeeded=true");
        var log  = Assert.Single(logs);
        Assert.Equal("Comment", log.EntityName);
        Assert.Equal(1, log.UserId);
    }

    // ---------- GET /admin/moderation-queue ----------
    // Content is flagged through the admin endpoint at the start of each test, so the seed stays all visible.
    // Seeded posts by creation time, oldest first: 12 (06:00), 11 (07:00), 9 / 10 / 13 (08:00, by id), 6 / 7 / 8 (09:00, by id),
    // then 1 to 5, which are created when the seed runs. Post 9 is charlie's (4), "Charlie Busy Post".
    // Seeded comments by creation time: 6 (08:30, diana (5) on post 9), then 1 to 5, created when the seed runs;
    // comment 1 is alice's (2) on post 2, comment 2 is bob's (3) on post 2, comment 5 has no author.

    private async Task<List<T>> GetQueueAsync<T>(string query)
    {
        var response = await _client.GetAsync($"api/admin/moderation-queue?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await ResponseReader.ReadListAsync<T>(response);
    }

    private async Task FlagPostsAsync(params int[] postIds)
    {
        foreach (var postId in postIds)
        {
            var response = await SetPostStatusAsync(postId, "Flagged");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private async Task FlagCommentsAsync(params int[] commentIds)
    {
        foreach (var commentId in commentIds)
        {
            var response = await SetCommentStatusAsync(commentId, "Flagged");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("type=post")]
    [InlineData("type=comment")]
    public async Task GetModerationQueue_WithoutToken_ReturnsUnauthorized(string query)
    {
        var response = await _client.GetAsync($"api/admin/moderation-queue?{query}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("type=post")]
    [InlineData("type=comment")]
    public async Task GetModerationQueue_AsOrdinaryUser_ReturnsForbidden(string query)
    {
        await SignInAsync("alice");

        var response = await _client.GetAsync($"api/admin/moderation-queue?{query}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetModerationQueue_ForPosts_ReturnsFlaggedPostsOldestFirst()
    {
        await SignInAsync("sandro_beno");
        await FlagPostsAsync(6, 9, 11, 12);

        var posts = await GetQueueAsync<PostSnapshotDto>("type=post");

        Assert.Equal([12, 11, 9, 6], posts.Select(post => post.Id));
    }

    [Fact]
    public async Task GetModerationQueue_ForPosts_ReturnsEachPostsSnapshot()
    {
        await SignInAsync("sandro_beno");
        await FlagPostsAsync(9);

        var posts = await GetQueueAsync<PostSnapshotDto>("type=post");

        var post = Assert.Single(posts);
        Assert.Equal(9, post.Id);
        Assert.Equal("Charlie Busy Post", post.Title);
        Assert.Equal("Liked and commented.", post.Text);
        Assert.Equal(4, post.Author.Id);
        Assert.Equal("charlie", post.Author.Username);
        Assert.Equal(new DateTime(2026, 9, 6, 8, 0, 0), post.UploadedAt);
        Assert.Empty(post.Images);
    }

    [Fact]
    public async Task GetModerationQueue_ForPostWithPhotos_ReturnsThePhotosInUploadOrder()
    {
        await SignInAsync("alice");
        var created = await _client.PostAsync("api/posts", PostForm.Create("Photo post", "With photos", ImageFormFiles.Png(1), ImageFormFiles.Jpeg(2)));
        var newPostId = (await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(created)).Id;
        await SignInAsync("sandro_beno");
        await FlagPostsAsync(newPostId);

        var posts = await GetQueueAsync<PostSnapshotDto>("type=post");

        var post = Assert.Single(posts);
        Assert.Equal([0, 1], post.Images.Select(image => image.OrderIndex));
        Assert.All(post.Images, image => Assert.StartsWith("/api/", image.Address));
    }

    [Fact]
    public async Task GetModerationQueue_ForComments_ReturnsFlaggedCommentsOldestFirst()
    {
        await SignInAsync("sandro_beno");
        await FlagCommentsAsync(2, 6, 1);

        var comments = await GetQueueAsync<CommentSnapshotDto>("type=comment");

        Assert.Equal([6, 1, 2], comments.Select(comment => comment.Id));
    }

    [Fact]
    public async Task GetModerationQueue_ForComments_ReturnsEachCommentsSnapshot()
    {
        await SignInAsync("sandro_beno");
        await FlagCommentsAsync(6);

        var comments = await GetQueueAsync<CommentSnapshotDto>("type=comment");

        var comment = Assert.Single(comments);
        Assert.Equal(6, comment.Id);
        Assert.Equal("Diana comments on Charlie's busy post.", comment.Text);
        Assert.Equal(5, comment.Author.Id);
        Assert.Equal("diana", comment.Author.Username);
        Assert.Equal(9, comment.PostId);
        Assert.Equal(new DateTime(2026, 9, 6, 8, 30, 0), comment.UploadedAt);
    }

    [Fact]
    public async Task GetModerationQueue_ForOneType_LeavesOutTheOther()
    {
        await SignInAsync("sandro_beno");
        await FlagPostsAsync(9);
        await FlagCommentsAsync(6);

        var posts    = await GetQueueAsync<PostSnapshotDto>("type=post");
        var comments = await GetQueueAsync<CommentSnapshotDto>("type=comment");

        Assert.Equal([9], posts.Select(post => post.Id));
        Assert.Equal([6], comments.Select(comment => comment.Id));
    }

    [Fact]
    public async Task GetModerationQueue_WhenNothingIsFlagged_ReturnsEmptyList()
    {
        await SignInAsync("sandro_beno");

        var posts    = await GetQueueAsync<PostSnapshotDto>("type=post");
        var comments = await GetQueueAsync<CommentSnapshotDto>("type=comment");

        Assert.Empty(posts);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetModerationQueue_NeverIncludesVisibleOrHiddenContent()
    {
        await SignInAsync("sandro_beno");
        await FlagPostsAsync(9);
        await FlagCommentsAsync(6);
        await SetPostStatusAsync(10, "Hidden");
        await SetCommentStatusAsync(1, "Hidden");

        var posts    = await GetQueueAsync<PostSnapshotDto>("type=post");
        var comments = await GetQueueAsync<CommentSnapshotDto>("type=comment");

        Assert.Equal([9], posts.Select(post => post.Id));
        Assert.Equal([6], comments.Select(comment => comment.Id));
    }

    [Theory]
    [InlineData("Visible")]
    [InlineData("Hidden")]
    public async Task GetModerationQueue_AfterPostIsDecided_NoLongerListsIt(string decision)
    {
        await SignInAsync("sandro_beno");
        await FlagPostsAsync(9, 10);

        await SetPostStatusAsync(9, decision);

        var posts = await GetQueueAsync<PostSnapshotDto>("type=post");
        Assert.Equal([10], posts.Select(post => post.Id));
    }

    [Theory]
    [InlineData("Visible")]
    [InlineData("Hidden")]
    public async Task GetModerationQueue_AfterCommentIsDecided_NoLongerListsIt(string decision)
    {
        await SignInAsync("sandro_beno");
        await FlagCommentsAsync(6, 1);

        await SetCommentStatusAsync(6, decision);

        var comments = await GetQueueAsync<CommentSnapshotDto>("type=comment");
        Assert.Equal([1], comments.Select(comment => comment.Id));
    }

    [Fact]
    public async Task GetModerationQueue_ForCommentWithoutActiveAuthor_ShowsPlaceholderUsername()
    {
        await SignInAsync("sandro_beno");
        await FlagCommentsAsync(5);

        var comments = await GetQueueAsync<CommentSnapshotDto>("type=comment");

        var comment = Assert.Single(comments);
        Assert.Null(comment.Author.Id);
        Assert.Equal(ResponseMessages.AccountDeletedUsername, comment.Author.Username);
    }

    [Fact]
    public async Task GetModerationQueue_ForPosts_ReturnsRequestedPage()
    {
        await SignInAsync("sandro_beno");
        await FlagPostsAsync(6, 9, 11, 12);

        var firstPage  = await GetQueueAsync<PostSnapshotDto>("type=post&pageNumber=1&pageSize=3");
        var secondPage = await GetQueueAsync<PostSnapshotDto>("type=post&pageNumber=2&pageSize=3");

        Assert.Equal([12, 11, 9], firstPage.Select(post => post.Id));
        Assert.Equal([6], secondPage.Select(post => post.Id));
    }

    [Fact]
    public async Task GetModerationQueue_ForComments_ReturnsRequestedPage()
    {
        await SignInAsync("sandro_beno");
        await FlagCommentsAsync(1, 2, 6);

        var secondPage = await GetQueueAsync<CommentSnapshotDto>("type=comment&pageNumber=2&pageSize=2");

        Assert.Equal([2], secondPage.Select(comment => comment.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("type=user")]
    [InlineData("type=0")]
    [InlineData("type=3")]
    public async Task GetModerationQueue_WithMissingOrUnknownType_ReturnsValidationProblem(string query)
    {
        await SignInAsync("sandro_beno");

        var response = await _client.GetAsync($"api/admin/moderation-queue?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errors\"", body); // the framework's validation ProblemDetails, not the envelope
    }

    // ---------- No Log row for a successful admin read ----------

    [Fact]
    public async Task GetLogsAndGetAccounts_WhenSuccessful_WriteNoLogRow()
    {
        await SignInAsync("sandro_beno");
        var logsBefore = await GetLogsAsync();

        await GetAccountsAsync();
        await GetAccountsAsync("state=Deactivated&username=erin");
        await GetQueueAsync<PostSnapshotDto>("type=post");
        await GetQueueAsync<CommentSnapshotDto>("type=comment");
        await GetLogsAsync("succeeded=false");
        var logsAfter = await GetLogsAsync();

        Assert.Equal(logsBefore.Select(log => log.Id), logsAfter.Select(log => log.Id));
    }
}
