using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Users;

public class EditUserDto
{
    [MinLength(Constants.UsernameMinLength), MaxLength(Constants.UsernameMaxlength)]
    public string Username { get; set; } = string.Empty;
    
    [MinLength(Constants.BioMinLength), MaxLength(Constants.BioMaxLength)]
    public string? Bio { get; set; }

    public DateTime DateOfBirth { get; set; }
}