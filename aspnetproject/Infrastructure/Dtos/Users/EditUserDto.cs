using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.Attributes;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Dtos.Users;

public class EditUserDto
{
    [MinLength(Constants.UsernameMinLength), MaxLength(Constants.UsernameMaxlength)]
    [Required]
    [RegularExpression(Constants.UsernameRegexPattern)]
    [DefaultValue("test")]
    public string   Username    { get; set; } = string.Empty;
    
    [ValidAge]
    [Required]
    [DefaultValue("2000-08-16T10:10:23.546Z")]
    public DateTime DateOfBirth { get; set; }
    
    [MinLength(Constants.BioMinLength), MaxLength(Constants.BioMaxLength)]
    public string?  Bio         { get; set; } = null;
}