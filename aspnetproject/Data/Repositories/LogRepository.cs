using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class LogRepository : BaseEntityRepository<Log>
{
    public LogRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        
    }

    public async Task<int> DeleteLogsAsync(OldLogsCleanupConfiguration config, CancellationToken cancellationToken)
    {
        int totalDeletedCount = 0;
        
        var cutoff = DateTime.UtcNow.AddDays(-config.RetentionDays);

        var logIds = await _dbSet
            .Where(log => log.CreatedAt < cutoff)
            .OrderBy(log => log.Id)
            .Take(config.BatchSize)
            .Select(log => log.Id)
            .ToListAsync(cancellationToken);

        if (logIds.Count > 0)
        {
            totalDeletedCount = await _dbSet
                .Where(log => logIds.Contains(log.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        return totalDeletedCount;
    }
}