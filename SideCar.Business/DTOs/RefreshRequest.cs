using System.ComponentModel.DataAnnotations;

namespace SideCar.Business.DTOs
{
    public class RefreshRequest
    {
        [Required]
        public required string RefreshToken { get; set; }
    }
}
