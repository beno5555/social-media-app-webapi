using System.ComponentModel.DataAnnotations;

namespace aspnetproject.Infrastructure.Dtos.Auth.Password;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? RedirectUrl { get; set; }
}