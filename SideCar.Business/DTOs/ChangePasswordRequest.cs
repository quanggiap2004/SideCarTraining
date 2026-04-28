using SideCar.Business.Helpers.ValidationRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.DTOs
{
    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; }
        [ValidatePassword]
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
