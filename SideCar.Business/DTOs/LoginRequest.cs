using SideCar.Business.Helpers.Constants;
using SideCar.Business.Helpers.ValidationRules;
using System.ComponentModel.DataAnnotations;

namespace SideCar.Business.DTOs
{
    public class LoginRequest
    {
        [Required]
        [StringLength(ValidationConstants.UsernameMaxLength, MinimumLength = ValidationConstants.UsernameMinLength, ErrorMessage = "Username length must be between 6 and 20 characters.")]
        public required string Username { get; set; }
        [ValidatePassword]
        public required string Password { get; set; }
    }
}
