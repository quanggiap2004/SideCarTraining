using SideCar.Business.Helpers.ValidationRules;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.DTOs
{
    public class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [ValidatePassword]
        public string NewPassword { get; set; } = string.Empty;
    }
}
