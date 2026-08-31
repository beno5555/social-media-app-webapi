using System.ComponentModel.DataAnnotations;

namespace aspnetproject.Infrastructure.Dtos.Auth.Password;

public class ResetPasswordDto
{
    [Required]
    public string Token       { get; set; } = string.Empty;
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}