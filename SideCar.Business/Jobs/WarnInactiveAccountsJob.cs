using Hangfire;
using Microsoft.Extensions.Logging;
using SideCar.Business.Helpers.Constants;
using SideCar.Business.Helpers.Utilities;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services;

namespace SideCar.Business.Jobs
{
    public class WarnInactiveAccountsJob(
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<WarnInactiveAccountsJob> logger,
        IDateTimerProvider _dateTimerProvider)
    {
        public async Task ExecuteAsync()
        {
            var cutoffDate = _dateTimerProvider.GetUtcNow.AddDays(-ProjectConstant.WarningDays);

            var candidates = await unitOfWork.Users.GetUsersForWarningAsync(cutoffDate);

            if (candidates.Count == 0)
            {
                logger.LogInformation("No accounts to send warning email.");
                return;
            }

            await unitOfWork.Users.BulkMarkWarningSentAsync(candidates.Select(x => x.Id).ToList());

            foreach (var user in candidates)
            {
                backgroundJobClient.Enqueue<IEmailService>(
                    job => job.SendWarningEmailAsync(user.Email, user.FullName));
            }

        }
    }
}
