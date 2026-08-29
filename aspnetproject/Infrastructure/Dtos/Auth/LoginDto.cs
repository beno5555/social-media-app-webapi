using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Auth;

public class LoginDto
{
    [MaxLength(Constants.UsernameMaxlength)]
    [DefaultValue("boba")]
    public string UniqueIdentifier { get; set; } = string.Empty;

    [MinLength(Constants.PasswordMinLength), MaxLength(Constants.PasswordMaxLength)]
    [DefaultValue("password123")]
    public string Password { get; set; } = string.Empty;
}