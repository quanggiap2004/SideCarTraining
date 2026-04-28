using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SideCar.Business.Helpers.ValidationRules
{
    public class ValidateEmail : ValidationAttribute
    {
        private static readonly Regex _emailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var email = value as string;
            if (string.IsNullOrWhiteSpace(email))
                return new ValidationResult("Email is required.");
            if (!_emailRegex.IsMatch(email))
                return new ValidationResult("Email must be a valid format (e.g. example@domain.com).");
            return ValidationResult.Success;
        }
    }
}
