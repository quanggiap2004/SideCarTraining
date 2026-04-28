using AutoMapper;
using FluentAssertions;
using Moq;
using SideCar.Business.DTOs;
using SideCar.Business.Entities;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services;
using SideCar.Business.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SideCar.Test
{
    public class UsersTest
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IEmailPublisher> _emailPublisher = new();
        private readonly Mock<IMapper> _mapper = new();

        private IUserService CreateService()
        {
            return new UserService(_userRepository.Object, _emailPublisher.Object, _mapper.Object);
        }

        private Users FakeUser() => new Users
        {
            Email = "yugiohpro1992@gmail.com",
            Fullname = "Giapdeptrai",
            Id = Guid.NewGuid(),
            PhoneNumber = "0943911515",
            Username = "Test",
        };

        [Theory]
        [InlineData("1 22222222 ")]
        [InlineData("0941922933 33333")]
        public async Task UpdateUserProfile_InvalidPhoneNumber_ReturnError(string phoneNumer)
        {
            var updateDto = new UpdateUserProfileDto()
            {
                Email = "yugiohpro1992@gmail.com",
                FullName = "Giapdeptrai",
                Id = Guid.Parse("979EC387-643C-F111-84C6-5414F3AAC8CC"),
                PhoneNumber = phoneNumer,
                UserName = "Test",
            };
            var user = FakeUser();
            _userRepository.Setup(r => r.FindUserByIdAsync(updateDto.Id.Value)).ReturnsAsync(user);
            var userService = CreateService();
            var act = async () => await userService.UpdateUserProfile(updateDto);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task DeleteUserAccount_Success_EnqueueEmail()
        {
            var user = FakeUser();
            _userRepository.Setup(u => u.FindUserByIdAsync(user.Id)).ReturnsAsync(user);
            _userRepository.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            var userService = CreateService();
            await userService.DeleteUserAccount(user.Id);
            _emailPublisher.Verify(p => p.QueueTemplateEmail(It.IsAny<TemplateEmailRequest>()), Times.Once);
        }

        [Theory]
        [InlineData("adfsdfasdf")]
        [InlineData("")]
        [InlineData("@gmail.com")]
        public async Task UpdateUserProfile_InvalidEmail_ReturnError(string email)
        {
            var user = FakeUser();
            var updateDto = new UpdateUserProfileDto()
            {
                Email = email,
                FullName = "Giapdeptrai",
                Id = Guid.Parse("979EC387-643C-F111-84C6-5414F3AAC8CC"),
                PhoneNumber = "0112345678",
                UserName = "Test12345",
            };
            _userRepository.Setup(u => u.FindUserByIdAsync(updateDto.Id.Value)).ReturnsAsync(user);
            _userRepository.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            var act = async () => await CreateService().UpdateUserProfile(updateDto);
            await act.Should().ThrowAsync<ValidationException>().WithMessage("*email*");
        }
    }
}
