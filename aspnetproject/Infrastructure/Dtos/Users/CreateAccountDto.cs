using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.Attributes;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Users;

public class CreateAccountDto
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
    [DefaultValue("2000-08-16T10:10:23.546Z")]
    public DateTime DateOfBirth { get; set; }
    
    [MinLength(Constants.BioMinLength), MaxLength(Constants.BioMaxLength)]
    public string?  Bio         { get; set; } = null;
}