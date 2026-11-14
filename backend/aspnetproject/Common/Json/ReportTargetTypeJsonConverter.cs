using System.Text.Json.Serialization;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Common.Json;

// Reads target types by name only, case-insensitively, like ModerationStatusJsonConverter: a number fails validation.
public class ReportTargetTypeJsonConverter : JsonStringEnumConverter<ReportTargetType>
{
    public ReportTargetTypeJsonConverter() : base(namingPolicy: null, allowIntegerValues: false)
    {

    }
}
