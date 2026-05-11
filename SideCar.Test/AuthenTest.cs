using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SideCar.Authen;
using SideCar.Business;
using SideCar.Business.DTOs;
using SideCar.Business.Entities;
using SideCar.Business.Enums;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services;
using SideCar.Business.Services.Interfaces;
using System.Linq.Expressions;

namespace SideCar.Test
{
    public class AuthenTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IAuthenRepository> _authenRepo = new();
        private readonly Mock<IUserActivityLogRepository> _activityLogRepo = new();
        private readonly Mock<IEmailPublisher> _emailPublisher = new();

        public AuthenTest()
        {
            _unitOfWork.Setup(u => u.Authen).Returns(_authenRepo.Object);
            _unitOfWork.Setup(u => u.ActivityLogs).Returns(_activityLogRepo.Object);
            _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        }

        private IAuthenService CreateService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "test-super-secret-key-32-chars!!",
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:ExpiryMinutes"] = "60",
                    ["Jwt:RefreshExpiryMinutes"] = "1440"
                })
                .Build();

            return new AuthenService(config, _unitOfWork.Object, _emailPublisher.Object);
        }

        [Fact]
        public async Task LoginAsync_InputValidCredentials_ReturnsAccessTokenAndRefreshToken()
        {
            var testUser = new Users
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123")
            };
            _authenRepo.Setup(repo => repo.GetByUsernameAsync("testuser")).ReturnsAsync(testUser);
            var result = await CreateService().LoginAsync("testuser", "CorrectPassword123");
            result.Should().NotBeNull();
            result!.AccessToken.Should().NotBeNullOrWhiteSpace();
            result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task RegisterAsync_CreateNewUser_ReturnsTrueAndSavesUser()
        {
            var request = new RegisterRequest
            {
                Username = "newuser",
                Password = "Password1",
                Email = "new@example.com",
                FullName = "Giap Quang",
                PhoneNumber = "1234567890",
            };

            _authenRepo.Setup(r => r.ExistsAsync("newuser", "new@example.com", "1234567890")).ReturnsAsync(false);
            _authenRepo.Setup(r => r.AddUserAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);
            _emailPublisher.Setup(p => p.QueueTemplateEmail(It.IsAny<TemplateEmailRequest>()));

            var result = await CreateService().RegisterAsync(request);

            result.Should().BeTrue();
            _authenRepo.Verify(r => r.AddUserAsync(It.Is<Users>(u =>
                u.Username == "newuser" &&
                u.Email == "new@example.com" &&
                u.PhoneNumber == "1234567890"
            )), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_RegisterSuccess_EnqueuesWelcomeEmail()
        {
            _authenRepo.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _authenRepo.Setup(r => r.AddUserAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);
            _emailPublisher.Setup(p => p.QueueTemplateEmail(It.IsAny<TemplateEmailRequest>()));

            var request = new RegisterRequest
            {
                Username = "newuser",
                Password = "Password1",
                Email = "new@example.com",
                FullName = "Giap123",
                PhoneNumber = "1234567890"
            };

            await CreateService().RegisterAsync(request);

            _emailPublisher.Verify(p => p.QueueTemplateEmail(It.IsAny<TemplateEmailRequest>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_InputValidCredentials_LogsSuccessActivity()
        {
            var userId = Guid.NewGuid();
            var testUser = new Users
            {
                Id = userId,
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123")
            };
            _authenRepo.Setup(repo => repo.GetByUsernameAsync("testuser")).ReturnsAsync(testUser);

            await CreateService().LoginAsync("testuser", "CorrectPassword123");

            _activityLogRepo.Verify(repo => repo.AddAsync(It.Is<UserActivityLog>(log =>
                log.UserId == userId &&
                log.ActivityType == ActivityType.Login
            )), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_InputValidToken_LogsSuccessActivity()
        {
            var userId = Guid.NewGuid();
            var testUsers = new Users
            {
                Id = userId,
                ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(10),
                ResetPasswordToken = "Random_hash_token"
            };

            _authenRepo.Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<Users, bool>>>())).ReturnsAsync(testUsers);
            
            await CreateService().ResetPasswordAsync("Random_hash_token", "NewPassword123");

            _activityLogRepo.Verify(repo => repo.AddAsync(It.Is<UserActivityLog>(log =>
                log.UserId == userId &&
                log.ActivityType == ActivityType.ResetPassword
            )), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_InputInvalidPassword_LogsFailedActivity()
        {
            var userId = Guid.NewGuid();
            var testUser = new Users
            {
                Id = userId,
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("InCorrectPassword")
            };
            _authenRepo.Setup(repo => repo.GetByUsernameAsync("testuser")).ReturnsAsync(testUser);

            await CreateService().LoginAsync("testuser", "WrongPassword");

            _activityLogRepo.Verify(repo => repo.AddAsync(It.Is<UserActivityLog>(log =>
                log.UserId == userId &&
                log.ActivityType == ActivityType.LoginFailed
            )), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}
