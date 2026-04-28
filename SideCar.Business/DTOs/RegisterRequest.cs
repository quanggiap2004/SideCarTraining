using SideCar.Business.Helpers.Constants;
using SideCar.Business.Helpers.ValidationRules;
using System.ComponentModel.DataAnnotations;

namespace SideCar.Business.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(ValidationConstants.UsernameMaxLength, MinimumLength = ValidationConstants.UsernameMinLength, ErrorMessage = "Username length must be between 6 and 20 characters.")]
        public required string Username { get; set; }
        [ValidatePassword]
        public required string Password { get; set; }
        [ValidateEmail]
        public required string Email { get; set; }
        [StringLength(ValidationConstants.FullNameMaxLength, MinimumLength = ValidationConstants.FullNameMinLength, ErrorMessage = "Full name length must be between 3 and 40 characters.")]
        public required string FullName { get; set; }
        [ValidatePhone]
        public required string PhoneNumber { get; set; }
    }
}
