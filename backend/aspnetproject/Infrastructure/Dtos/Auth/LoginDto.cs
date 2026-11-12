using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Auth;

public class LoginDto
{
    [MaxLength(Constants.UsernameMaxlength)]
    [Required]
    [DefaultValue("steve.runte197970")]
    public string UniqueIdentifier { get; set; } = string.Empty;

    [MinLength(Constants.PasswordMinLength), MaxLength(Constants.PasswordMaxLength)]
    [Required]
    [DefaultValue("password123")]
    public string Password { get; set; } = string.Empty;
}