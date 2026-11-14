using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Infrastructure.Dtos.Reports;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

// Callers: sandro_beno (1) is the administrator; alice (2), bob (3), charlie (4) and diana (5) are ordinary users.
// Seed state used here (see TestDataSeeder):
//   post 1 is alice's, post 2 is bob's, post 3 is charlie's
//   comment 1 is alice's (on post 2), comment 4 is bob's (on post 1)
//   erin_deactivated (6) is deactivated, frank_deleted (7) is soft-deleted
// Seeded reports, newest first (times rise with the id):
//   6: alice   -> post 3 (charlie's)  rejected, reviewed by frank_deleted (7)
//   5: frank   -> post 3              pending (the reporter is soft-deleted)
//   4: diana   -> post 999            pending, the target does not exist
//   3: bob     -> user 4 (charlie)    resolved, reviewed by sandro_beno (1)
//   2: charlie -> comment 1 (alice's) pending
//   1: alice   -> post 2 (bob's)      pending
[Collection("Database")]
public class ReportControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture             _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    public ReportControllerTests(DatabaseFixture dbFixture)
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

    // ---------- POST /reports ----------

    [Theory]
    [InlineData("Post", 2)]    // bob's post
    [InlineData("Comment", 4)] // bob's comment
    [InlineData("User", 3)]    // bob
    public async Task Create_ForAnotherUsersTarget_ReturnsPendingReport(string targetType, int targetId)
    {
        await SignInAsync("charlie");

        var response = await CreateReportAsync(targetType, targetId, "Spam");
        var body     = await response.Content.ReadFromJsonAsync<ApplicationResponse<StandardReportDto>>();
        var report   = body!.Data!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ResponseMessages.ReportCreated, body.Message);
        Assert.Equal(targetType, report.TargetType.ToString());
        Assert.Equal(targetId, report.TargetId);
        Assert.Equal("Spam", report.Reason);
        Assert.Equal("Pending", report.Status.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithoutReason_ReturnsBadRequest(string? reason)
    {
        await SignInAsync("charlie");

        var response = await CreateReportAsync("Post", 2, reason);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReasonOverLengthLimit_ReturnsBadRequest()
    {
        await SignInAsync("charlie");

        var response = await CreateReportAsync("Post", 2, new string('x', Constants.ReportReasonMaxLength + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReasonAtLengthLimit_ReturnsOk()
    {
        await SignInAsync("charlie");

        var response = await CreateReportAsync("Post", 2, new string('x', Constants.ReportReasonMaxLength));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Message")]
    [InlineData("1")]
    public async Task Create_UnknownTargetType_ReturnsBadRequest(string targetType)
    {
        await SignInAsync("charlie");

        var response = await CreateReportAsync(targetType, 2, "Spam");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Post", 9999)]
    [InlineData("Comment", 9999)]
    [InlineData("User", 9999)]
    [InlineData("User", 6)] // erin_deactivated
    [InlineData("User", 7)] // frank_deleted
    public async Task Create_UnknownTarget_ReturnsNotFoundMessage(string targetType, int targetId)
    {
        await SignInAsync("charlie");

        var response = await CreateReportAsync(targetType, targetId, "Spam");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(NotFoundMessage(targetType), await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_HiddenPostOfAnotherUser_ReturnsNotFoundMessage()
    {
        await SetStatusAsync("posts", 2, "Hidden"); // bob's post
        await SignInAsync("charlie");

        var response = await CreateReportAsync("Post", 2, "Spam");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.PostNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_HiddenCommentOfAnotherUser_ReturnsNotFoundMessage()
    {
        await SetStatusAsync("comments", 4, "Hidden"); // bob's comment
        await SignInAsync("charlie");

        var response = await CreateReportAsync("Comment", 4, "Spam");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.CommentNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    // A visible comment on a hidden post would otherwise reveal the post.
    [Fact]
    public async Task Create_VisibleCommentOnHiddenPost_ReturnsNotFoundMessage()
    {
        await SetStatusAsync("posts", 1, "Hidden"); // alice's post, which holds bob's comment 4
        await SignInAsync("charlie");

        var response = await CreateReportAsync("Comment", 4, "Spam");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.CommentNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Theory]
    [InlineData("Post", 1, ResponseMessages.CannotReportOwnContent)]    // alice's post
    [InlineData("Comment", 1, ResponseMessages.CannotReportOwnContent)] // alice's comment
    [InlineData("User", 2, ResponseMessages.CannotReportOneself)]       // alice
    public async Task Create_OwnTarget_ReturnsBadRequest(string targetType, int targetId, string expectedMessage)
    {
        await SignInAsync("alice");

        var response = await CreateReportAsync(targetType, targetId, "Spam");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expectedMessage, await ResponseReader.ReadMessageAsync(response));
    }

    // The author may still read their hidden post, so it is their own content, not missing.
    [Fact]
    public async Task Create_OwnHiddenPost_ReturnsCannotReportOwnContent()
    {
        await SetStatusAsync("posts", 1, "Hidden"); // alice's post
        await SignInAsync("alice");

        var response = await CreateReportAsync("Post", 1, "Spam");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.CannotReportOwnContent, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_TargetCallerAlreadyHasPendingReportOn_ReturnsBadRequest()
    {
        await SignInAsync("alice");

        var response = await CreateReportAsync("Post", 2, "Again"); // report 1 is alice's pending report on post 2

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.ReportAlreadyPending, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_SameTargetTwice_RefusesTheSecond()
    {
        await SignInAsync("diana");

        var first  = await CreateReportAsync("Post", 2, "Spam");
        var second = await CreateReportAsync("Post", 2, "Spam");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal(ResponseMessages.ReportAlreadyPending, await ResponseReader.ReadMessageAsync(second));
    }

    [Fact]
    public async Task Create_TargetAnotherUserHasPendingReportOn_ReturnsOk()
    {
        await SignInAsync("charlie");

        var response = await CreateReportAsync("Post", 2, "Spam"); // alice's report 1 on post 2 is pending

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_TargetCallersEarlierReportWasDecided_ReturnsOk()
    {
        await SignInAsync("alice");

        var response = await CreateReportAsync("Post", 3, "Still spam"); // alice's report 6 on post 3 was rejected

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await CreateReportAsync("Post", 2, "Spam");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- GET /reports ----------

    [Fact]
    public async Task GetAll_WithoutFilters_ReturnsPendingReportsNewestFirst()
    {
        await SignInAsync("sandro_beno");

        var reports = await GetReportsAsync("");

        Assert.Equal([5, 4, 2, 1], reports.Select(report => report.Id));
        Assert.All(reports, report => Assert.Equal(ReportStatus.Pending, report.Status));
    }

    [Theory]
    [InlineData("Resolved", new[] { 3 })]
    [InlineData("Rejected", new[] { 6 })]
    [InlineData("Pending", new[] { 5, 4, 2, 1 })]
    public async Task GetAll_ByStatus_ReturnsOnlyThatStatus(string status, int[] expectedIds)
    {
        await SignInAsync("sandro_beno");

        var reports = await GetReportsAsync($"status={status}");

        Assert.Equal(expectedIds, reports.Select(report => report.Id));
    }

    [Theory]
    [InlineData("targetType=Post", new[] { 5, 4, 1 })]
    [InlineData("targetType=Comment", new[] { 2 })]
    [InlineData("targetType=User", new int[0])]
    [InlineData("targetType=User&status=Resolved", new[] { 3 })]
    [InlineData("targetType=Post&status=Rejected", new[] { 6 })]
    public async Task GetAll_ByTargetType_CombinesWithStatus(string query, int[] expectedIds)
    {
        await SignInAsync("sandro_beno");

        var reports = await GetReportsAsync(query);

        Assert.Equal(expectedIds, reports.Select(report => report.Id));
    }

    [Theory]
    [InlineData(1, new[] { 5, 4 })]
    [InlineData(2, new[] { 2, 1 })]
    [InlineData(3, new int[0])]
    public async Task GetAll_Paged_ReturnsThatPage(int pageNumber, int[] expectedIds)
    {
        await SignInAsync("sandro_beno");

        var reports = await GetReportsAsync($"pageNumber={pageNumber}&pageSize=2");

        Assert.Equal(expectedIds, reports.Select(report => report.Id));
    }

    [Theory]
    [InlineData("status=Archived")]
    [InlineData("targetType=Message")]
    [InlineData("pageSize=0")]
    public async Task GetAll_InvalidQuery_ReturnsBadRequest(string query)
    {
        await SignInAsync("sandro_beno");

        var response = await _client.GetAsync($"api/reports?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_PostReport_CarriesReporterReasonAndPostSnapshot()
    {
        await SignInAsync("sandro_beno");

        var report = (await GetReportsAsync("")).Single(row => row.Id == 1);

        Assert.Equal(ReportTargetType.Post, report.TargetType);
        Assert.Equal("alice", report.Reporter.Username);
        Assert.Equal("Alice reports Bob's post.", report.Reason);
        Assert.Equal(new DateTime(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc), report.CreatedAt);
        Assert.Null(report.ReviewedBy);
        Assert.Null(report.ReviewedAt);
        Assert.False(report.TargetMissing);
        Assert.Equal(2, report.Post!.Id);
        Assert.Equal("Bob Post", report.Post.Title);
        Assert.Equal("Bob's post should appear in Alice's feed.", report.Post.PostContent);
        Assert.Equal("bob", report.Post.Author.Username);
        Assert.NotNull(report.Post.Images);
        Assert.Null(report.Comment);
        Assert.Null(report.User);
    }

    [Fact]
    public async Task GetAll_CommentReport_CarriesCommentSnapshot()
    {
        await SignInAsync("sandro_beno");

        var report = (await GetReportsAsync("targetType=Comment")).Single();

        Assert.False(report.TargetMissing);
        Assert.Equal(1, report.Comment!.Id);
        Assert.Equal("Alice comments on Bob's post.", report.Comment.Content);
        Assert.Equal("alice", report.Comment.Author.Username);
        Assert.Equal(2, report.Comment.PostId);
        Assert.Null(report.Post);
    }

    [Fact]
    public async Task GetAll_DecidedUserReport_CarriesReviewerAndUserSnapshot()
    {
        await SignInAsync("sandro_beno");

        var report = (await GetReportsAsync("status=Resolved")).Single();

        Assert.Equal("sandro_beno", report.ReviewedBy!.Username);
        Assert.Equal(new DateTime(2026, 9, 7, 11, 0, 0, DateTimeKind.Utc), report.ReviewedAt);
        Assert.Equal("charlie", report.User!.Username);
        Assert.Equal("Unrelated active user", report.User.Bio);
    }

    [Fact]
    public async Task GetAll_TargetThatNoLongerExists_IsShownAsMissing()
    {
        await SignInAsync("sandro_beno");

        var report = (await GetReportsAsync("")).Single(row => row.Id == 4); // post 999

        Assert.True(report.TargetMissing);
        Assert.Null(report.Post);
    }

    [Fact]
    public async Task GetAll_DeletedTarget_IsShownAsMissing()
    {
        await SignInAsync("bob");
        var deleted = await _client.DeleteAsync("api/posts/2");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        await SignInAsync("sandro_beno");

        var report = (await GetReportsAsync("")).Single(row => row.Id == 1); // alice's report on post 2

        Assert.True(report.TargetMissing);
        Assert.Null(report.Post);
    }

    // The administrator reviews hidden content, so hiding a reported post must not make it read as missing.
    [Fact]
    public async Task GetAll_HiddenTarget_StillCarriesSnapshot()
    {
        await SetStatusAsync("posts", 2, "Hidden");

        var report = (await GetReportsAsync("")).Single(row => row.Id == 1);

        Assert.False(report.TargetMissing);
        Assert.True(report.Post!.IsHidden);
    }

    [Fact]
    public async Task GetAll_ReporterSoftDeleted_ShowsDeletedUsername()
    {
        await SignInAsync("sandro_beno");

        var report = (await GetReportsAsync("")).Single(row => row.Id == 5); // frank_deleted's report

        Assert.Equal(ResponseMessages.AccountDeletedUsername, report.Reporter.Username);
    }

    [Fact]
    public async Task GetAll_AsOrdinaryUser_ReturnsForbidden()
    {
        await SignInAsync("alice");

        var response = await _client.GetAsync("api/reports");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/reports");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<List<AdminReportDto>> GetReportsAsync(string query)
    {
        var response = await _client.GetAsync($"api/reports?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await ResponseReader.ReadListAsync<AdminReportDto>(response);
    }

    private static string NotFoundMessage(string targetType)
    {
        return targetType switch
        {
            "Post"    => ResponseMessages.PostNotFound,
            "Comment" => ResponseMessages.CommentNotFound,
            _         => ResponseMessages.UserNotFound
        };
    }

    // Signs in as the administrator to change the status; the test then signs in as the caller it acts as.
    private async Task SetStatusAsync(string contentPath, int id, string status)
    {
        await SignInAsync("sandro_beno");
        var response = await _client.PutAsJsonAsync($"api/admin/{contentPath}/{id}/moderation", new { Status = status });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpResponseMessage> CreateReportAsync(string targetType, int targetId, string? reason)
    {
        return await _client.PostAsJsonAsync("api/reports", new { TargetType = targetType, TargetId = targetId, Reason = reason });
    }

    private async Task SignInAsync(string username)
    {
        var token = await AuthTestHelper.LoginAsync(_client, username, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
    }
}
