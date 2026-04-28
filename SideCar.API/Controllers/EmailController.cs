using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideCar.Business.Services;

namespace SideCar.API.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class EmailController(IBackgroundJobClient backgroundJob, IEmailService _emailService) : ControllerBase
    {
        [HttpPost("send-email")]
        public IActionResult SendEmail([FromQuery] string email, [FromQuery] string subject, [FromQuery] string message)
        {
            backgroundJob.Enqueue<IEmailService>(x => x.SendEmail(email, subject, message));
            return Accepted();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("upload-email")]
        public async Task<IActionResult> UploadEmailTemplate([FromForm] EmailTemplate emailTemplate)
        {
            using var fileStream = emailTemplate.file.OpenReadStream();
            await _emailService.UploadEmailTemplateAsync(emailTemplate.templateName, fileStream);
            return Ok(new { Message = $"Template '{emailTemplate.templateName}' uploaded successfully"});
        }

        [AllowAnonymous]
        [HttpGet("preview-email/{templateName}")]
        public async Task<IActionResult> PreviewEmailTemplate(string templateName)
        {
            var html = await _emailService.GetRenderedTemplate(templateName, new Dictionary<string, string>
            {
                { "FullName", "Preview User" },
                { "Username", "previewuser" },
                { "Email", "preview@example.com" }
            });
            return Content(html, "text/html");
        }
    }

    public record EmailTemplate(string templateName, IFormFile file);
}
