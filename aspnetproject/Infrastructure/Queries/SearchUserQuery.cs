using System.ComponentModel.DataAnnotations;
using Constants = aspnetproject.Common.ProjectConstants.Constants;

namespace aspnetproject.Infrastructure.Queries;

public class SearchUserQuery : PageQuery
{
    [MinLength(Constants.UsernameMinLength), MaxLength(Constants.UsernameMaxlength)]
    public string Username { get; set; } = string.Empty;
}