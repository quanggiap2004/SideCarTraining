using Hangfire;
using Microsoft.Extensions.Logging;
using SideCar.Business.Helpers.Constants;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services;

namespace SideCar.Business.Jobs
{
    public class WarnInactiveAccountsJob(
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<WarnInactiveAccountsJob> logger)
    {
        public async Task ExecuteAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-ProjectConstant.WarningDays);

            var candidates = await unitOfWork.Users.GetUsersForWarningAsync(cutoffDate);

            if (candidates.Count == 0)
            {
                logger.LogInformation("No account to sen warn email");
                return;
            }
            
            foreach (var user in candidates)
            {
                backgroundJobClient.Enqueue<IEmailService>(
                    job => job.SendWarningEmailAsync(user.Email, user.FullName));
                await unitOfWork.Users.MarkWarningSentAsync(user.Id);
                
            }
        }
    }
}
