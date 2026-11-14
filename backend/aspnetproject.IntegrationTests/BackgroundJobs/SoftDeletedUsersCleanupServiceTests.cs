using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Dtos.Reports;
using aspnetproject.Infrastructure.Services.BackgroundJobs;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace aspnetproject.IntegrationTests.BackgroundJobs;

// Runs one sweep of the job directly, resolved from the host's services: the test host removes hosted services, so no
// timer ever fires here. A failed sweep is logged, not thrown, so each test checks what the sweep left behind.
[Collection("Database")]
public class SoftDeletedUsersCleanupServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture             _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    public SoftDeletedUsersCleanupServiceTests(DatabaseFixture dbFixture)
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

    // ---------- one sweep ----------
    // frank_deleted (7) was soft-deleted on 2026-08-12, past the default 20-day retention, so the sweep purges him.
    // Report 5 is frank's own pending report; report 6 is alice's rejected report that frank reviewed.
    // sandro_beno (1) is the administrator who reads the reports.

    [Fact]
    public async Task Sweep_ForPurgedUsersReports_DeletesThem()
    {
        await SweepAsync();

        var pending = await GetReportsAsync("status=Pending");

        Assert.DoesNotContain(pending, report => report.Id == 5);
        Assert.Equal([4, 2, 1], pending.Select(report => report.Id));
    }

    [Fact]
    public async Task Sweep_ForReportsThePurgedUserReviewed_KeepsThemWithoutReviewer()
    {
        await SweepAsync();

        var report = (await GetReportsAsync("status=Rejected")).Single();

        Assert.Equal(6, report.Id);
        Assert.Null(report.ReviewedBy!.Id);
        Assert.Equal(ResponseMessages.AccountDeletedUsername, report.ReviewedBy.Username);
        Assert.NotNull(report.ReviewedAt);
    }

    private async Task SweepAsync()
    {
        await _factory.Services.GetRequiredService<SoftDeletedUsersCleanupService>().RunOnceAsync(CancellationToken.None);
    }

    private async Task<List<AdminReportDto>> GetReportsAsync(string query)
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        return await ResponseReader.ReadListAsync<AdminReportDto>(await _client.GetAsync($"api/reports?{query}"));
    }
}
