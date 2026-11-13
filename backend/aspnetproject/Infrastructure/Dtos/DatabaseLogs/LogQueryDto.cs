using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Queries;

namespace aspnetproject.Infrastructure.Dtos.DatabaseLogs;

// Every filter is optional; the ones that are set combine with AND.
public class LogQueryDto : PageQuery, IValidatableObject
{
    public bool? AuthorizedRequest { get; set; }
    public int?  UserId            { get; set; }

    public bool? Succeeded { get; set; }

    [MaxLength(Constants.LogEntityNameMaxLength)]
    public string? EntityName { get; set; }
    public int?    EntityId   { get; set; }

    // Matches any part of the action name, so "PostService" finds every PostService method.
    [MaxLength(Constants.LogActionMaxLength)]
    public string? Action { get; set; }

    // Both ends are inclusive and compared as UTC.
    public DateTime? From { get; set; }
    public DateTime? To   { get; set; }

    // A reversed range is a shape error, so it is refused as validation ProblemDetails like the other query rules.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        bool isRangeReversed = From is not null && To is not null && From > To;
        if (isRangeReversed)
        {
            results.Add(new ValidationResult(ResponseMessages.InvalidDateRange, [nameof(From), nameof(To)]));
        }

        return results;
    }
}
