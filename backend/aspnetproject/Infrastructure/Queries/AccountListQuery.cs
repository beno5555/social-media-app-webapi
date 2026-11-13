using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Infrastructure.Queries;

// Both filters are optional; when both are set they combine with AND.
public class AccountListQuery : PageQuery
{
    public AccountState? State { get; set; }

    // Matches any part of the username.
    [MaxLength(Constants.UsernameMaxlength)]
    public string? Username { get; set; }
}
