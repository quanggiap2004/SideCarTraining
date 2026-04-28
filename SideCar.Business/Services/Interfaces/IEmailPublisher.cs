using SideCar.Business.DTOs;

namespace SideCar.Business.Services.Interfaces
{
    public interface IEmailPublisher
    {
        void QueueTemplateEmail(TemplateEmailRequest request);
    }
}
