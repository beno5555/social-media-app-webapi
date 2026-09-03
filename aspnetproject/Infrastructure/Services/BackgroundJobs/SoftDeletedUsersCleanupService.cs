using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Base;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Common;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;
using aspnetproject.Infrastructure.Services.Logging;
using Microsoft.Extensions.Options;

namespace aspnetproject.Infrastructure.Services.BackgroundJobs;

public class SoftDeletedUsersCleanupService : PeriodicHostedService
{
    private readonly SoftDeletedUsersCleanupConfiguration _config;
    
    public SoftDeletedUsersCleanupService(IServiceScopeFactory scopeFactory, IOptions<SoftDeletedUsersCleanupConfiguration> config) : base(scopeFactory, config.Value.Interval)
    {
        _config = config.Value;
    }

    protected override async Task RunCycleAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        var repo = serviceProvider.GetRequiredService<UserRepository>();
        var dbLogger =  serviceProvider.GetRequiredService<DatabaseLogger>();

        var cutoff = DateTime.UtcNow.AddDays(-_config.RetentionDays);

        SoftDeleteCleanupResult batchResult;
        var totalResult      = new SoftDeleteCleanupResult();

        do
        {
            batchResult = await repo.DeleteSoftDeletedUsersBatchAsync(cutoff, _config, stoppingToken);
            totalResult += batchResult;
        }
        while(batchResult.UsersDeletedCount > 0 && !stoppingToken.IsCancellationRequested);
        
        if (totalResult.UsersDeletedCount > 0)
        {
            await LogResultAsync(dbLogger, true, nameof(User),
                $"Removed {totalResult.UsersDeletedCount} soft deleted users past retention. " +
                $"(Messages: {totalResult.MessagesDeletedCount}, Friendships: {totalResult.FriendshipsDeletedCount}, Posts: {totalResult.PostsDeletedCount})");
        }
    }
}