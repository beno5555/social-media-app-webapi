using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Infrastructure.Queries;

// The queue holds posts and comments in separate tables, so each call reads one type.
// Nullable so that a missing type fails validation instead of binding to a default.
public class ModerationQueueQuery : PageQuery
{
    [Required]
    [EnumDataType(typeof(ModerationContentType))]
    public ModerationContentType? Type { get; set; }
}
