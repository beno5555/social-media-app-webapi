using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Base;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;
using aspnetproject.Infrastructure.Services.Logging;
using aspnetproject.Infrastructure.Services.Storage;
using Microsoft.Extensions.Options;

namespace aspnetproject.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// The backstop for photo files no post or message refers to: those left by a crash between saving the files and saving
/// their owner, by a best-effort delete that failed, or by a purge that stopped partway.
/// </summary>
public class OrphanImagesCleanupService : PeriodicHostedService
{
    private readonly OrphanImagesCleanupConfiguration _config;

    public OrphanImagesCleanupService(IOptions<OrphanImagesCleanupConfiguration> config, IServiceScopeFactory scopeFactory)
        : base(scopeFactory, config.Value.Interval)
    {
        _config = config.Value;
    }

    protected override async Task RunCycleAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        var imageStorage = serviceProvider.GetRequiredService<ImageStorage>();
        var repo         = serviceProvider.GetRequiredService<ImageRepository>();
        var dbLogger     = serviceProvider.GetRequiredService<DatabaseLogger>();

        // An upload writes its files before it saves their post or message, so a file younger than MinimumAge may belong
        // to an owner that is not saved yet. Older files had all that time to get their row.
        var writtenBefore = DateTime.UtcNow - _config.MinimumAge;
        var batches       = imageStorage.ListImageAddressesWrittenBefore(writtenBefore).Chunk(_config.BatchSize).ToList();

        int totalDeleted = 0;

        for (int index = 0; index < batches.Count && !stoppingToken.IsCancellationRequested; index++)
        {
            totalDeleted += await DeleteUnreferencedAsync(batches[index], repo, imageStorage, stoppingToken);
        }

        if (totalDeleted > 0)
        {
            await LogResultAsync(dbLogger, true, nameof(BaseImage), $"Removed {totalDeleted} image files no post or message refers to");
        }
    }

    private async Task<int> DeleteUnreferencedAsync(
        string[]          addresses,
        ImageRepository   repo,
        ImageStorage      imageStorage,
        CancellationToken stoppingToken
        )
    {
        var referenced   = (await repo.GetReferencedAddressesAsync(addresses, stoppingToken)).ToHashSet();
        var unreferenced = addresses.Where(address => !referenced.Contains(address));

        return await imageStorage.DeleteAsync(unreferenced);
    }
}
