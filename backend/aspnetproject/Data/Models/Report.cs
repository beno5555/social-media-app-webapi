using System.ComponentModel.DataAnnotations.Schema;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Data.Models;

// The target is a type and an id, not a foreign key, so a report outlives its target and then reads as missing.
public class Report : BaseEntity
{
    public ReportTargetType TargetType { get; set; }
    public int              TargetId   { get; set; }

    public string       Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public int   ReporterUserId { get; set; }
    public User? ReporterUser   { get; set; }

    public int?  ReviewedByUserId { get; set; }
    public User? ReviewedByUser   { get; set; }

    [Column(TypeName = "datetime2(3)")]
    public DateTime? ReviewedAt { get; set; }
}
