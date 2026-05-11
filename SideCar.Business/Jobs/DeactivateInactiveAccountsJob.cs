using Hangfire;
using Microsoft.Extensions.Logging;
using SideCar.Business.Helpers.Constants;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services;

namespace SideCar.Business.Jobs
{
    public class DeactivateInactiveAccountsJob(
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<DeactivateInactiveAccountsJob> logger)
    {
        public async Task ExecuteAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-ProjectConstant.InactiveDays);

            var candidates = await unitOfWork.Users.GetInactiveUserCandidatesAsync(cutoffDate);

            if (candidates.Count == 0)
            {
                logger.LogInformation("No inactive accounts to deactivate.");
                return;
            }

            List<Guid> ids = candidates.Select(x => x.Id).ToList();
            var affected = await unitOfWork.Users.BulkDeactivateAccountAsync(ids, cutoffDate);

            foreach (var (id, email) in candidates)
            {
                backgroundJobClient.Enqueue<IEmailService>(
                    job => job.SendDeactivationEmailAsync(email, id));
            }
        }
    }
}
