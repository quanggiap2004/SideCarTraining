using SideCar.Business.Helpers.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Helpers.ValidationRules
{
    public class ValidateUserName : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var username = value as string;
            if (string.IsNullOrWhiteSpace(username))
                throw new ValidationException("Username is required.");
            if (username.Length < ProjectConstant.UsernameMinLength || username.Length > ProjectConstant.UsernameMaxLength)
                throw new ValidationException($"Username must be between {ProjectConstant.UsernameMinLength} and {ProjectConstant.UsernameMaxLength} characters.");
            return ValidationResult.Success;
        }
    }
}