using aspnetproject.Data;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Base;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;
using aspnetproject.Infrastructure.Services.Logging;
using Microsoft.Extensions.Options;

namespace aspnetproject.Infrastructure.Services.BackgroundJobs;

public class RefreshTokenCleanupService : PeriodicHostedService
{
    public RefreshTokenCleanupService(IOptions<RefreshTokenCleanupConfiguration> config, IServiceScopeFactory scopeFactory) 
        : base(scopeFactory, config.Value.Interval)
    {
    }

    protected override async Task RunCycleAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        var repo = serviceProvider.GetRequiredService<RefreshTokenRepository>();
        var dbLogger = serviceProvider.GetRequiredService<DatabaseLogger>();

        int deletedCount = await repo.DeleteExpiredAsync(stoppingToken);

        if (deletedCount > 0)
        {
            await LogResultAsync(dbLogger, true, nameof(RefreshToken), null);
        }
    }
}