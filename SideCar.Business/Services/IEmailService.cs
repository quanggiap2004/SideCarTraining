using Hangfire;
using SideCar.Business.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SideCar.Business.Services
{
    public interface IEmailService
    {
        Task SendEmail(string email, string subject, string message);
        Task SendEmailTemplate(TemplateEmailRequest request);
        Task UploadEmailTemplateAsync(string templateName, Stream fileStream);
        Task<string> GetRenderedTemplate(string templateName, Dictionary<string, string> placeholders);
        Task SendDeactivationEmailAsync(string email, Guid userId);

        [AutomaticRetry(Attempts = 3)]
        Task SendWarningEmailAsync(string email, string fullName);
    }
}
