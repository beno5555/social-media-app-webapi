using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using aspnetproject.Common.Json;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Infrastructure.Dtos.Moderation;

// Status names only, read case-insensitively (ModerationStatusJsonConverter). Nullable so that a missing status fails validation instead of binding to 0.
public class SetModerationStatusDto
{
    [Required]
    [EnumDataType(typeof(ModerationStatus))]
    [JsonConverter(typeof(ModerationStatusJsonConverter))]
    public ModerationStatus? Status { get; set; }
}
