using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SideCar.Authen;
using SideCar.Business.DTOs;
using SideCar.Business.Entities;
using SideCar.Business.Enums;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services.Interfaces;

namespace SideCar.Test
{
    public class AuthenTest
    {
        private readonly Mock<IAuthenRepository> _repo = new();
        private readonly Mock<IEmailPublisher> _emailPublisher = new();

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

            return new AuthenService(config, _repo.Object, _emailPublisher.Object);
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

            _repo.Setup(r => r.GetByUsernameAsync("johndoe")).ReturnsAsync(user);
            _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var sut = CreateService();
            var result = await sut.LoginAsync("johndoe", plainPassword);

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

            _repo.Setup(r => r.ExistsAsync("newuser", "new@example.com", "1234567890")).ReturnsAsync(false);
            _repo.Setup(r => r.AddUserAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);
            _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
            _emailPublisher.Setup(p => p.QueueTemplateEmail(It.IsAny<TemplateEmailRequest>()));

            var result = await CreateService().RegisterAsync(request);

            result.Should().BeTrue();
            _repo.Verify(r => r.AddUserAsync(It.Is<Users>(u =>
                u.Username == "newuser" &&
                u.Email == "new@example.com" &&
                u.PhoneNumber == "1234567890"
            )), Times.Once);
            _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_Success_EnqueuesWelcomeEmail()
        {
            _repo.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _repo.Setup(r => r.AddUserAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);
            _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
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
