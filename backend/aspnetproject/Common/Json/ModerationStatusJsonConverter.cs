using System.Text.Json.Serialization;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Common.Json;

// Reads moderation statuses by name only, case-insensitively. The default enum converter also accepts numbers,
// so 3 (or "3") would set Hidden; here a number fails validation like an unknown name.
public class ModerationStatusJsonConverter : JsonStringEnumConverter<ModerationStatus>
{
    public ModerationStatusJsonConverter() : base(namingPolicy: null, allowIntegerValues: false)
    {

    }
}
