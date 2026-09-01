using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Base;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;
using aspnetproject.Infrastructure.Services.Logging;
using Microsoft.Extensions.Options;

namespace aspnetproject.Infrastructure.Services.BackgroundJobs;

public class SoftDeletedUserCleanupService : PeriodicHostedService
{
    private readonly SoftDeleteCleanupOptions _config;
    
    public SoftDeletedUserCleanupService(IServiceScopeFactory scopeFactory, IOptions<SoftDeleteCleanupOptions> config) : base(scopeFactory, config.Value.Interval)
    {
        _config = config.Value;
    }

    protected override async Task RunCycleAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        var repo = serviceProvider.GetRequiredService<UserRepository>();
        var dbLogger =  serviceProvider.GetRequiredService<DatabaseLogger>();

        var cutoff = DateTime.UtcNow.AddDays(-_config.RetentionDays);

        int deletedCount = await repo.DeleteSoftDeletedUsersAsync(cutoff, _config.BatchSize, stoppingToken);
    }
}