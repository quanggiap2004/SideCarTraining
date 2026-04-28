using System.ComponentModel.DataAnnotations;

namespace SideCar.Business.DTOs
{
    public class TemplateEmailRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Subject { get; set; }

        [Required]
        public required string TemplateName { get; set; }

        public Dictionary<string, string> Placeholders { get; set; } = new();
    }
}
