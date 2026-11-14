using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using aspnetproject.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class ReportRepository : BaseEntityRepository<Report>
{
    public ReportRepository(ApplicationDbContext dbContext) : base(dbContext)
    {

    }

    public async Task<bool> HasPendingReportAsync(int reporterId, ReportTargetType targetType, int targetId)
    {
        return await ExistsAsync(report => report.ReporterUserId == reporterId
                                           && report.TargetType  == targetType
                                           && report.TargetId    == targetId
                                           && report.Status      == ReportStatus.Pending);
    }

    // Newest first. Id breaks ties between reports written in the same millisecond, so paging is deterministic.
    public async Task<List<Report>> GetReportsAsync(ReportListQuery reportQuery)
    {
        var query = Query(track: false)
            .Where(report => report.Status == reportQuery.Status);

        if (reportQuery.TargetType is not null)
        {
            query = query.Where(report => report.TargetType == reportQuery.TargetType);
        }

        var orderedQuery = query
            .OrderByDescending(report => report.CreatedAt)
            .ThenByDescending(report => report.Id);

        return await Page(orderedQuery, reportQuery.PageNumber, reportQuery.PageSize).ToListAsync();
    }

    // Filters ignored so a reporter or reviewer whose account is deactivated or soft-deleted still loads, and the mapper can name that state.
    protected override IQueryable<Report> Query(bool track = true)
    {
        var query = _dbSet
            .IgnoreQueryFilters()
            .Include(report => report.ReporterUser)
            .Include(report => report.ReviewedByUser);

        return track ? query : query.AsNoTracking();
    }
}
