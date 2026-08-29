using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.Attributes;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Auth;

public class RegisterDto
{
    [MinLength(Constants.UsernameMinLength), MaxLength(Constants.UsernameMaxlength)]
    [RegularExpression(Constants.UsernameRegexPattern)]
    [DefaultValue("test")]
    public string   Username    { get; set; } = string.Empty;
    
    [MinLength(Constants.EmailMinLength), MaxLength(Constants.EmailMaxLength)]
    [RegularExpression(Constants.EmailRegexPattern)]
    [EmailAddress]
    [DefaultValue("test@gmail.com")]
    public string   Email       { get; set; } = string.Empty;
    
    [MinLength(Constants.PasswordMinLength), MaxLength(Constants.PasswordMaxLength)]
    [DefaultValue("password123")]
    public string   Password    { get; set; } = string.Empty;
    
    [ValidAge]
    public DateTime DateOfBirth { get; set; }
    
    [MinLength(Constants.BioMinLength), MaxLength(Constants.BioMaxLength)]
    public string?  Bio         { get; set; } = null;
}