using Hangfire;
using SideCar.Business.DTOs;
using SideCar.Business.Services.Interfaces;

namespace SideCar.Business.Services
{
    public class HangfireEmailPublisher(IBackgroundJobClient _backgroundJobClient) : IEmailPublisher
    {
        public void QueueTemplateEmail(TemplateEmailRequest request)
            => _backgroundJobClient.Enqueue<IEmailService>(x => x.SendEmailTemplate(request));
    }
}
