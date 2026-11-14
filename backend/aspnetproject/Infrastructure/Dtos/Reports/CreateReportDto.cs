using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using aspnetproject.Common.Json;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Infrastructure.Dtos.Reports;

// Target type names only, read case-insensitively (ReportTargetTypeJsonConverter). The nullable fields make a missing
// value fail validation instead of binding to 0.
public class CreateReportDto
{
    [Required]
    [EnumDataType(typeof(ReportTargetType))]
    [JsonConverter(typeof(ReportTargetTypeJsonConverter))]
    public ReportTargetType? TargetType { get; set; }

    [Required]
    public int? TargetId { get; set; }

    [Required]
    [MaxLength(Constants.ReportReasonMaxLength)]
    public string Reason { get; set; } = string.Empty;
}
