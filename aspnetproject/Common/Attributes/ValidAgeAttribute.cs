using System.ComponentModel.DataAnnotations;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Common.Attributes;

public class ValidAgeAttribute : ValidationAttribute
{
    private const int MinAge = Constants.MinAge;
    private const int MaxAge = Constants.MaxAge;

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not DateTime dob)
        {
            return ValidationResult.Success;
        }

        var today = DateTime.UtcNow.Date;
        if (dob.Date > today)
        {
            return new ValidationResult("Date of birth cannot be in the future.");
        }

        var age = today.Year - dob.Year;
        if (dob.Date > today.AddYears(-age))
        {
            age--;
        }

        if (age < MinAge)
        {
            return new ValidationResult($"Must be at least {MinAge} years old.");
        }

        if (age > MaxAge)
        {
            return new ValidationResult($"Age exceeds the maximum allowed ({MaxAge}).");
        }

        return ValidationResult.Success;
    }
}