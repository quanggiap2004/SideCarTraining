using SideCar.Business.Helpers.Constants;
using SideCar.Business.Helpers.ValidationRules;
using System.ComponentModel.DataAnnotations;

namespace SideCar.Business.DTOs
{
    public class LoginRequest
    {
        [ValidateUserName]
        public required string Username { get; set; }
        [ValidatePassword]
        public required string Password { get; set; }
    }
}
