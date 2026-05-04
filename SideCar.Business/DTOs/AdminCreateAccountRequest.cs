using SideCar.Business.Helpers.Constants;
using SideCar.Business.Helpers.ValidationRules;
using System.ComponentModel.DataAnnotations;

namespace SideCar.Business.DTOs
{
    public class AdminCreateAccountRequest
    {
        [Required]
        [StringLength(ProjectConstant.UsernameMaxLength, MinimumLength = ProjectConstant.UsernameMinLength, ErrorMessage = "Username length must be between 6 and 20 characters.")]
        public required string Username { get; set; }
        [ValidatePassword]
        public required string Password { get; set; }
        [EmailAddress]
        public required string Email { get; set; }
        [StringLength(ProjectConstant.FullNameMaxLength, MinimumLength = ProjectConstant.FullNameMinLength, ErrorMessage = "Full name length must be between 3 and 40 characters.")]
        public required string FullName { get; set; }
        [ValidatePhone]
        public required string PhoneNumber { get; set; }
        public required string Role { get; set; }
    }
}
