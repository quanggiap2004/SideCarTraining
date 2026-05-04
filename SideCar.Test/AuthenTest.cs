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
                    ["Jwt:ExpiryMinutes"] = "60"
                })
                .Build();

            return new AuthenService(config, _unitOfWork.Object, _emailPublisher.Object);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokens()
        {
            var plainPassword = "Password1";
            var user = new Users
            {
                Id = Guid.NewGuid(),
                Username = "johndoe",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                Role = Roles.User
            };

            _authenRepo.Setup(r => r.GetByUsernameAsync("johndoe")).ReturnsAsync(user);
            _activityLogRepo.Setup(r => r.AddAsync(It.IsAny<UserActivityLog>())).Returns(Task.CompletedTask);

            var result = await CreateService().LoginAsync("johndoe", plainPassword);

            result.Should().NotBeNull();
            result!.AccessToken.Should().NotBeNullOrWhiteSpace();
            result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task RegisterAsync_NewUser_ReturnsTrueAndSavesUser()
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
        public async Task RegisterAsync_Success_EnqueuesWelcomeEmail()
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
    }
}
