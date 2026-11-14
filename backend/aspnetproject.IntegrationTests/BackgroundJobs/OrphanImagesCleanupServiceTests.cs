using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
using aspnetproject.Infrastructure.Dtos.Images;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.Infrastructure.Services.BackgroundJobs;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace aspnetproject.IntegrationTests.BackgroundJobs;

// Runs one sweep of the job directly, resolved from the host's services: the test host removes hosted services, so no
// timer ever fires here.
[Collection("Database")]
public class OrphanImagesCleanupServiceTests : IAsyncLifetime
{
    // A fixed time in the past, so it is always older than the default MinimumAge (1 hour).
    private static readonly DateTime LongAgo = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    private const string JobLogsQuery = $"api/admin/logs?pageSize=100&action={nameof(OrphanImagesCleanupService)}";

    private readonly DatabaseFixture             _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    public OrphanImagesCleanupServiceTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory   = new CustomWebApplicationFactory();
        _client    = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());

    // Disposing the host releases its settings-file watchers; each one holds an inotify instance, and Linux caps them per user.
    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task LoginAsAsync(string username)
    {
        var token = await AuthTestHelper.LoginAsync(_client, username, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
    }

    // ---------- one sweep ----------
    // The seed has no post or message photos, so a file only has a row if the test uploads it.
    // diana (5) posts; alice (2) and bob (3) are accepted friends, so alice can message bob.
    // sandro_beno (1) is the administrator who reads the job's Log rows.

    [Fact]
    public async Task Sweep_ForOldFileNothingRefersTo_DeletesIt()
    {
        string orphan = WriteStoredFile(".jpg", LongAgo);

        await SweepAsync();

        Assert.False(File.Exists(orphan));
    }

    [Fact]
    public async Task Sweep_ForOldFilesOfPostAndMessagePhotos_KeepsThem()
    {
        await LoginAsAsync("diana");
        var post = await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(
            await _client.PostAsync("api/posts", PostForm.Create("Album", null, ImageFormFiles.Png(1), ImageFormFiles.Jpeg(2))));
        await LoginAsAsync("alice");
        var message = await ResponseReader.ReadDataAsync<SentMessageDto>(
            await _client.PostAsync("api/messages/3", MessageForm.Create(null, ImageFormFiles.WebP(3))));
        var referencedFiles = post.Images.Concat(message.Images).Select(ToFilePath).ToList();
        referencedFiles.ForEach(path => File.SetLastWriteTimeUtc(path, LongAgo));

        await SweepAsync();

        Assert.Equal(3, referencedFiles.Count);
        Assert.All(referencedFiles, path => Assert.True(File.Exists(path)));
        Assert.Empty(await GetJobLogsAsync());
    }

    [Fact]
    public async Task Sweep_ForYoungFileNothingRefersTo_KeepsIt()
    {
        string youngOrphan = WriteStoredFile(".png", DateTime.UtcNow);

        await SweepAsync();

        Assert.True(File.Exists(youngOrphan));
        Assert.Empty(await GetJobLogsAsync());
    }

    [Fact]
    public async Task Sweep_ForOldFilesNotNamedAsStoredImages_LeavesThemAlone()
    {
        string[] others =
        [
            WriteFile($"{Guid.NewGuid():N}.txt", LongAgo),
            WriteFile($"{Guid.NewGuid():N}.gif", LongAgo),
            WriteFile("photo.jpg", LongAgo),
            WriteFile(Path.Combine("nested", $"{Guid.NewGuid():N}.jpg"), LongAgo)
        ];

        await SweepAsync();

        Assert.All(others, path => Assert.True(File.Exists(path)));
        Assert.Empty(await GetJobLogsAsync());
    }

    [Fact]
    public async Task Sweep_WithMissingUploadFolder_RemovesNothingAndLogsNoFailure()
    {
        Directory.Delete(_factory.UploadFolder, recursive: true);

        await SweepAsync();

        Assert.Empty(await GetJobLogsAsync());
    }

    [Fact]
    public async Task Sweep_ThatRemovesFiles_WritesOneLogRowWithTheCount()
    {
        WriteStoredFile(".jpg", LongAgo);
        WriteStoredFile(".webp", LongAgo);

        await SweepAsync();

        var log = Assert.Single(await GetJobLogsAsync());
        Assert.True(log.Succeeded);
        Assert.Contains("Removed 2 ", log.Details);
    }

    [Fact]
    public async Task Sweep_ThatRemovesNothing_WritesNoLogRow()
    {
        WriteStoredFile(".jpg", DateTime.UtcNow);

        await SweepAsync();

        Assert.Empty(await GetJobLogsAsync());
    }

    // ---------- settings ----------

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-01:00:00")]
    [InlineData("00:00:59")]
    public async Task Startup_WithMinimumAgeUnderOneMinute_FailsToStart(string minimumAge)
    {
        await using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["CleanupConfiguration:OrphanImages:MinimumAge"] = minimumAge
        });

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.IsType<OptionsValidationException>(exception);
    }

    // ---------- helpers ----------

    private async Task SweepAsync()
    {
        var job = _factory.Services.GetRequiredService<OrphanImagesCleanupService>();
        await job.RunOnceAsync(CancellationToken.None);
    }

    // The job's rows, failed cycles included. Read by the administrator on a client of its own, so _client keeps its login.
    private async Task<List<LogDisplayDto>> GetJobLogsAsync()
    {
        using var adminClient = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAsync(adminClient, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(adminClient, token);

        var response = await adminClient.GetAsync(JobLogsQuery);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ListResponse<LogDisplayDto>>();
        return body!.Data;
    }

    private string ToFilePath(ImageDto image)
    {
        return Path.Combine(_factory.UploadFolder, Path.GetFileName(image.Address));
    }

    // A file named the way ImageStorage names uploads, last written at writtenAt.
    private string WriteStoredFile(string extension, DateTime writtenAt)
    {
        return WriteFile($"{Guid.NewGuid():N}{extension}", writtenAt);
    }

    private string WriteFile(string relativePath, DateTime writtenAt)
    {
        string path = Path.Combine(_factory.UploadFolder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, TestImages.Jpeg());
        File.SetLastWriteTimeUtc(path, writtenAt);
        return path;
    }
}
