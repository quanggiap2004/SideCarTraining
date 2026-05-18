using Amazon.Runtime.Internal.Util;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Moq;
using SideCar.Business.DTOs;
using SideCar.Business.Helpers.Constants;
using SideCar.Business.Helpers.Utilities;
using SideCar.Business.Jobs;
using SideCar.Business.Repositories;
using SideCar.Business.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Test
{
    public class DeactivateInactiveAccountsJobTest
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
        private readonly Mock<ILogger<DeactivateInactiveAccountsJob>> _logger = new();
        private readonly Mock<IDateTimerProvider> _dateTimerProvider = new();
        private static readonly DateTime fixedTime = new(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime expectedCutOff = fixedTime.AddDays(-ProjectConstant.InactiveDays);

        public DeactivateInactiveAccountsJobTest()
        {
            _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _unitOfWork.Setup(u => u.Users).Returns(_userRepo.Object);
            _dateTimerProvider.Setup(u => u.GetUtcNow).Returns(fixedTime);
        }
        private DeactivateInactiveAccountsJob createDeactivatedAccountsJob =>
            new DeactivateInactiveAccountsJob(_unitOfWork.Object, _backgroundJobClient.Object, _logger.Object, _dateTimerProvider.Object);

        [Fact]
        public async Task ExecuteAsync_WhenCall_UseCorrectCutOffDate()
        {
            var expectedCutoffDate = fixedTime.AddDays(-ProjectConstant.InactiveDays);
            _userRepo.Setup(repo => repo.GetInactiveUserCandidatesAsync(It.IsAny<DateTime>())).ReturnsAsync([]);

            await createDeactivatedAccountsJob.ExecuteAsync();

            _userRepo.Verify(repo => repo.GetInactiveUserCandidatesAsync(expectedCutoffDate), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenHaveCandidates_EnqueueEmailForEachCandidates()
        {
            var candidatesList = new List<InactiveUserCandidate>
            {
                new() { Id = Guid.NewGuid(), Email = "test1@example.com" },
                new() { Id = Guid.NewGuid(), Email = "test2@example.com" }
            };
            _userRepo.Setup(repo => repo.GetInactiveUserCandidatesAsync(It.IsAny<DateTime>())).ReturnsAsync(candidatesList);
            _userRepo.Setup(repo => repo.BulkDeactivateAccountAsync(It.IsAny<List<Guid>>(), It.IsAny<DateTime>())).ReturnsAsync(candidatesList.Count);
            await createDeactivatedAccountsJob.ExecuteAsync();

            _backgroundJobClient.Verify(client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Exactly(candidatesList.Count));
        }

        [Fact]
        public async Task ExecuteAsync_WhenCandidatesFound_DeactivatesWithSameCutoffDate()
        {
            var candidatesList = new List<InactiveUserCandidate>
            {
                new() { Id = Guid.NewGuid(), Email = "test1@example.com" },
                new() { Id = Guid.NewGuid(), Email = "test2@example.com" }
            };

            _userRepo.Setup(x => x.GetInactiveUserCandidatesAsync(It.IsAny<DateTime>()))
                     .ReturnsAsync(candidatesList);
            _userRepo.Setup(x => x.BulkDeactivateAccountAsync(It.IsAny<List<Guid>>(), It.IsAny<DateTime>()))
                     .ReturnsAsync(candidatesList.Count);

            await createDeactivatedAccountsJob.ExecuteAsync();

            _userRepo.Verify(
                x => x.BulkDeactivateAccountAsync(
                    It.Is<List<Guid>>(ids => ids.SequenceEqual(candidatesList.Select(c => c.Id))),
                    expectedCutOff),
                Times.Once());
        }

        [Fact]
        public async Task ExecuteAsync_WhenHaveNoCandidates_DontEnqueueEmail()
        {
            _userRepo.Setup(u => u.GetInactiveUserCandidatesAsync(It.IsAny<DateTime>())).ReturnsAsync([]);

            await createDeactivatedAccountsJob.ExecuteAsync();

            _backgroundJobClient.Verify(u => u.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
        }
    }
}
