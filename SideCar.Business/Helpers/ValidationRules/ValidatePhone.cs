using SideCar.Business.Helpers.Constants;
using System.ComponentModel.DataAnnotations;

namespace SideCar.Business.Helpers.ValidationRules
{
    public class ValidatePhone : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var phone = value as string;
            if (string.IsNullOrWhiteSpace(phone))
                return new ValidationResult("Phone number is required.");
            if (!phone.All(char.IsDigit) || phone.Length != ProjectConstant.PhoneLength)
                return new ValidationResult($"Phone number must be exactly {ProjectConstant.PhoneLength} digits.");
            return ValidationResult.Success;
        }
    }
}
