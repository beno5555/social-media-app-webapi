using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Base;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;
using aspnetproject.Infrastructure.Services.Logging;
using Microsoft.Extensions.Options;

namespace aspnetproject.Infrastructure.Services.BackgroundJobs;

public class OldLogsCleanupService : PeriodicHostedService
{
    private readonly OldLogsCleanupConfiguration _config;
    
    public OldLogsCleanupService(IOptions<OldLogsCleanupConfiguration> config, IServiceScopeFactory scopeFactory) : base(scopeFactory, config.Value.Interval)
    {
        _config = config.Value;
    }

    protected override async Task RunCycleAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        var dbLogger = serviceProvider.GetRequiredService<DatabaseLogger>();
        var repo     = serviceProvider.GetRequiredService<LogRepository>();

        var totalDeleted = 0;
        int batchDeleted = 0;

        do
        {
            batchDeleted = await repo.DeleteLogsAsync(_config, stoppingToken);
            totalDeleted += batchDeleted;
        } while (batchDeleted > 0 && !stoppingToken.IsCancellationRequested);

        if (totalDeleted > 0)
        {
            await LogResultAsync(dbLogger, true, nameof(Log), $"Logs Deleted: {totalDeleted}");
        }
    }
}