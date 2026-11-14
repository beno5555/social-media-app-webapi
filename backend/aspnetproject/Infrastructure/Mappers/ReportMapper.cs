using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Reports;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Mappers;

public static class ReportMapper
{
    public static Report ToEntity(int reporterId, CreateReportDto createReportDto)
    {
        return new Report
        {
            ReporterUserId = reporterId,
            TargetType     = createReportDto.TargetType!.Value,
            TargetId       = createReportDto.TargetId!.Value,
            Reason         = createReportDto.Reason
        };
    }

    public static StandardReportDto ToStandardDisplay(Report report)
    {
        return new StandardReportDto
        {
            Id         = report.Id,
            TargetType = report.TargetType,
            TargetId   = report.TargetId,
            Reason     = report.Reason,
            Status     = report.Status,
            CreatedAt  = report.CreatedAt
        };
    }

    /// <summary>
    /// assumes applied .Include() on ReporterUser and ReviewedByUser. Pass the one target that matches
    /// TargetType, loaded for review, or none when it no longer exists.
    /// </summary>
    public static AdminReportDto ToAdminDisplay(Report report, Post? post, Comment? comment, User? user)
    {
        // A decided report whose reviewer was purged keeps its ReviewedAt, so it still shows a (deleted) reviewer.
        bool isDecided     = report.ReviewedAt is not null;
        bool targetMissing = post is null && comment is null && user is null;

        return new AdminReportDto
        {
            Id         = report.Id,
            TargetType = report.TargetType,
            TargetId   = report.TargetId,
            Reason     = report.Reason,
            Status     = report.Status,
            Reporter   = new MinimalUserDto
            {
                Id       = report.ReporterUserId,
                Username = UsernameMapper.ResolveUsername(report.ReporterUser, report.ReporterUserId)
            },
            ReviewedBy = isDecided
                ? new MinimalUserDto
                {
                    Id       = report.ReviewedByUserId,
                    Username = UsernameMapper.ResolveUsername(report.ReviewedByUser, report.ReviewedByUserId)
                }
                : null,
            CreatedAt     = report.CreatedAt,
            ReviewedAt    = report.ReviewedAt,
            TargetMissing = targetMissing,
            Post          = post    is null ? null : SnapshotMapper.ToPostSnapshot(post),
            Comment       = comment is null ? null : SnapshotMapper.ToCommentSnapshot(comment),
            User          = user    is null ? null : SnapshotMapper.ToUserSnapshot(user)
        };
    }
}
