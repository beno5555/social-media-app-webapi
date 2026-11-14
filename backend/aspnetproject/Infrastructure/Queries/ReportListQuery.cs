using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Infrastructure.Queries;

// Pending reports unless a status is given; every target type unless one is given. Both filters combine with AND.
public class ReportListQuery : PageQuery
{
    [EnumDataType(typeof(ReportStatus))]
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    [EnumDataType(typeof(ReportTargetType))]
    public ReportTargetType? TargetType { get; set; }
}
