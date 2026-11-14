using System.Text.Json.Serialization;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Infrastructure.Dtos.Reports;

// The reporter's view of a report they just made.
public class StandardReportDto
{
    public int Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<ReportTargetType>))]
    public ReportTargetType TargetType { get; set; }
    public int              TargetId   { get; set; }

    public string Reason { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter<ReportStatus>))]
    public ReportStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
