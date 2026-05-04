using SideCar.Business.Helpers.Constants;
using System.ComponentModel.DataAnnotations;

namespace SideCar.Business.Helpers.ValidationRules
{
    public class ValidatePassword : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var password = value as string;
            if (string.IsNullOrWhiteSpace(password))
                return new ValidationResult("Password is required.");
            if (password.Length < ProjectConstant.PasswordMinLength || password.Length > ProjectConstant.PasswordMaxLength)
                return new ValidationResult($"Password must be between {ProjectConstant.PasswordMinLength} and {ProjectConstant.PasswordMaxLength} characters.");
            if (!password.Any(char.IsUpper))
                return new ValidationResult("Password must contain at least one uppercase letter.");
            return ValidationResult.Success;
        }
    }
}
