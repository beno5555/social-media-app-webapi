using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.Attributes;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Users;

public class CreateAccountDto
{
    [MinLength(Constants.UsernameMinLength), MaxLength(Constants.UsernameMaxlength)]
    [RegularExpression(Constants.UsernameRegexPattern)]
    public string   Username    { get; set; } = string.Empty;
    
    [MinLength(Constants.EmailMinLength), MaxLength(Constants.EmailMaxLength)]
    [RegularExpression(Constants.EmailRegexPattern)]
    public string   Email       { get; set; } = string.Empty;
    
    [MinLength(Constants.PasswordMinLength), MaxLength(Constants.PasswordMaxLength)]
    public string   Password    { get; set; } = string.Empty;
    
    [ValidAge]
    public DateTime DateOfBirth { get; set; }
    
    [MinLength(Constants.BioMinLength), MaxLength(Constants.BioMaxLength)]
    public string?  Bio         { get; set; } = null;
}