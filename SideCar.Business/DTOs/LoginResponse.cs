using System;
using System.Collections.Generic;
using System.Text;

namespace SideCar.Business.DTOs
{
    public class LoginResponse
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }

    }
}
