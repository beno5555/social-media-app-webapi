using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Auth.Password;

public class ResetPasswordDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(Constants.PasswordMinLength), MaxLength(Constants.PasswordMaxLength)]
    public string NewPassword { get; set; } = string.Empty;
}