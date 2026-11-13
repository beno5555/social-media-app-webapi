using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class LogRepository : BaseEntityRepository<Log>
{
    public LogRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        
    }

    public async Task<List<Log>> SearchLogsAsync(LogQueryDto logQuery)
    {
        var query = Query(track: false);

        if (logQuery.UserId is not null)
        {
            query = query.Where(log => log.UserId == logQuery.UserId);
        }

        if (logQuery.AuthorizedRequest is not null)
        {
            query = query.Where(log => log.AuthorizedRequest == logQuery.AuthorizedRequest);
        }

        if (logQuery.Succeeded is not null)
        {
            query = query.Where(log => log.Succeeded == logQuery.Succeeded);
        }

        if (logQuery.EntityName is not null)
        {
            query = query.Where(log => log.EntityName == logQuery.EntityName);
        }

        if (logQuery.EntityId is not null)
        {
            query = query.Where(log => log.EntityId == logQuery.EntityId);
        }

        if (logQuery.Action is not null)
        {
            query = query.Where(log => log.Action.Contains(logQuery.Action));
        }

        if (logQuery.From is not null)
        {
            query = query.Where(log => log.CreatedAt >= logQuery.From);
        }

        if (logQuery.To is not null)
        {
            query = query.Where(log => log.CreatedAt <= logQuery.To);
        }

        // Id breaks ties between rows written in the same millisecond, so paging is deterministic.
        return await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip((logQuery.PageNumber - 1) * logQuery.PageSize)
            .Take(logQuery.PageSize)
            .ToListAsync();
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