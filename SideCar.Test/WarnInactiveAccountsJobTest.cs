using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Moq;
using SideCar.Business.DTOs;
using SideCar.Business.Helpers.Constants;
using SideCar.Business.Helpers.Utilities;
using SideCar.Business.Jobs;
using SideCar.Business.Repositories.Interfaces;

namespace SideCar.Test
{
    public class WarnInactiveAccountsJobTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
        private readonly Mock<ILogger<WarnInactiveAccountsJob>> _logger = new();
        private readonly Mock<IDateTimerProvider> _dateTimerProvider = new();
        private static readonly DateTime fixedTime = new(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime expectedCutOff = fixedTime.AddDays(-ProjectConstant.WarningDays);

        public WarnInactiveAccountsJobTest()
        {
            _unitOfWork.Setup(u => u.Users).Returns(_userRepo.Object);
            _dateTimerProvider.Setup(u => u.GetUtcNow).Returns(fixedTime);
        }

        private WarnInactiveAccountsJob createWarnInactiveAccountJob =>
            new WarnInactiveAccountsJob(_unitOfWork.Object, _backgroundJobClient.Object, _logger.Object, _dateTimerProvider.Object);

        [Fact]
        public async Task ExecuteAsync_WhenCalled_UseCorrectCutOffDate()
        {
            _userRepo.Setup(u => u.GetUsersForWarningAsync(It.IsAny<DateTime>())).ReturnsAsync([]);

            await createWarnInactiveAccountJob.ExecuteAsync();

            _userRepo.Verify(u => u.GetUsersForWarningAsync(expectedCutOff), Times
                .Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenHaveCandidates_EnqueueEmailForEachCandidates()
        {
            var candidates = new List<UserForWarning>
            {
                new UserForWarning {Id = Guid.NewGuid(), FullName = "test01", Email = "test01@gmail.com" },
                new UserForWarning{Id = Guid.NewGuid(), FullName = "test02", Email = "test02@gmail.com" },
                new UserForWarning{Id = Guid.NewGuid(), FullName = "test03",Email = "test03@gmail.com" }
            };
            _userRepo.Setup(u => u.GetUsersForWarningAsync(It.IsAny<DateTime>())).ReturnsAsync(candidates);
            _userRepo.Setup(u => u.BulkMarkWarningSentAsync(It.IsAny<List<Guid>>())).ReturnsAsync(candidates.Count);

            await createWarnInactiveAccountJob.ExecuteAsync();

            _backgroundJobClient.Verify(u => u.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Exactly(candidates.Count));
        }


        [Fact]
        public async Task ExecuteAsync_WhenHaveNoCandidates_DontEnqueueEmail()
        {
            _userRepo.Setup(u => u.GetUsersForWarningAsync(It.IsAny<DateTime>())).ReturnsAsync([]);

            await createWarnInactiveAccountJob.ExecuteAsync();

            _backgroundJobClient.Verify(u => u.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
        }
    }
}
