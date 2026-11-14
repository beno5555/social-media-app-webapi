using System.Text.Json.Serialization;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Infrastructure.Dtos.Moderation;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Dtos.Reports;

// An administrator's view of a report. At most one snapshot is set, the one matching TargetType;
// none is set when the target no longer exists, and TargetMissing says so.
public class AdminReportDto
{
    public int Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<ReportTargetType>))]
    public ReportTargetType TargetType { get; set; }
    public int              TargetId   { get; set; }

    public string Reason { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter<ReportStatus>))]
    public ReportStatus Status { get; set; }

    public MinimalUserDto  Reporter   { get; set; } = null!;
    public MinimalUserDto? ReviewedBy { get; set; }

    public DateTime  CreatedAt  { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public bool                TargetMissing { get; set; }
    public PostSnapshotDto?    Post          { get; set; }
    public CommentSnapshotDto? Comment       { get; set; }
    public UserSnapshotDto?    User          { get; set; }
}
