using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
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

    // ---------- No Log row for a successful admin read ----------

    [Fact]
    public async Task GetLogsAndGetAccounts_WhenSuccessful_WriteNoLogRow()
    {
        await SignInAsync("sandro_beno");
        var logsBefore = await GetLogsAsync();

        await GetAccountsAsync();
        await GetAccountsAsync("state=Deactivated&username=erin");
        await GetLogsAsync("succeeded=false");
        var logsAfter = await GetLogsAsync();

        Assert.Equal(logsBefore.Select(log => log.Id), logsAfter.Select(log => log.Id));
    }
}
