using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SideCar.Business.DTOs;
using SideCar.Business.DTOs.Params;
using SideCar.Business.Entities;
using SideCar.Business.Enums;
using SideCar.Business.Helpers.Constants;
using SideCar.Business.Helpers.Exceptions;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;


namespace SideCar.Business.Services
{
    public class UserService(IUserRepository userRepository, IEmailPublisher _emailPublisher, IMapper _mapper) : IUserService
    {
        public async Task<bool> DeleteUserAccount(Guid id)
        {
            var userAccount = await userRepository.FindUserByIdAsync(id);
            if(userAccount == null)
            {
                throw new KeyNotFoundException("Account with id:" + id);
            }
            if(userAccount.IsDeleted)
            {
                return false;
            }
            if(userAccount.Role == Roles.Admin)
            {
                throw new BusinessException("Cannot delete admin account");
            }
            userAccount.IsDeleted = true;
            await userRepository.SaveChangesAsync();

            var templateRequest = new TemplateEmailRequest
            {
                Email = userAccount.Email,
                Subject = "Account deactivation",
                TemplateName = "account-deleted",
                Placeholders = new Dictionary<string, string>
                {
                    { "FullName", userAccount.Fullname },
                    { "Email", userAccount.Email },
                    { "Username", userAccount.Username },
                    { "DeleteAt", DateTime.UtcNow.ToString("dd MMM yyyy, HH:mm UTC") }
                }
            };
            _emailPublisher.QueueTemplateEmail(templateRequest);
            return true;
        }

        public async Task<(IEnumerable<UserResponseDto>, int totalCount)> GetAllUserAsync(QueryUserParams userParams)
        {
            var userList = await userRepository.GetPagedUsersAsync(userParams);
            var totalCount = await userRepository.CountUsersAsync(userParams);
            var userResponseList = _mapper.Map<List<UserResponseDto>>(userList);
            return (userResponseList, totalCount);
        }

        public async Task<UserProfileDto> GetUserProfileDtoAsync(Guid id)
        {
            var userAccount = await userRepository.FindUserByIdAsync(id);
            if(userAccount == null)
            {
                throw new ArgumentException("Cannot find account with id: " + id);
            }
            return _mapper.Map<UserProfileDto>(userAccount);
        }

        public async Task<bool> UpdateUserAccount(UpdateAccountDto account)
        {
            if (account.Id == null)
            {
                throw new KeyNotFoundException("Id is null");
            }
            var updateUser = await userRepository.FindUserByIdAsync(account.Id.Value);
            if(updateUser == null)
            {
                throw new KeyNotFoundException("Account with id:" + account.Id);
            }
            if (account.PhoneNumber != null)
            {
                ValidatePhone(account.PhoneNumber);
                updateUser.PhoneNumber = account.PhoneNumber;
            }
            if (account.FullName != null)
            {
                ValidateFullName(account.FullName);
                updateUser.Fullname = account.FullName;
            }
            if (account.UserName != null)
            {
                ValidateUsername(account.UserName);
                updateUser.Username = account.UserName;
            }
            await userRepository.SaveChangesAsync();
            return true;
        }

        public async Task UpdateUserProfile(UpdateUserProfileDto account)
        {
            var accountUpdate = await userRepository.FindUserByIdAsync(account.Id.Value);
            if(accountUpdate == null)
            {
                throw new KeyNotFoundException("Account with id: " + account.Id + " not found"); 
            }
            if (account.PhoneNumber != null)
            {
                ValidatePhone(account.PhoneNumber);
                accountUpdate.PhoneNumber = account.PhoneNumber;
            }
            if (account.FullName != null)
            {
                ValidateFullName(account.FullName);
                accountUpdate.Fullname = account.FullName;
            }
            if (account.UserName != null)
            {
                ValidateUsername(account.UserName);
                accountUpdate.Username = account.UserName;
            }
            if(account.Email != null)
            {
                ValidateEmail(account.Email);
                accountUpdate.Email = account.Email;
            }
            await userRepository.SaveChangesAsync();
        }

        private bool ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ValidationException("Phone number is required.");
            if (!phone.All(char.IsDigit) || phone.Length != ValidationConstants.PhoneLength)
                throw new ValidationException($"Phone number must be exactly {ValidationConstants.PhoneLength} digits.");

            return true;
        }

        private bool ValidateFullName(string fullName)
        {
            if (fullName.Length < ValidationConstants.FullNameMinLength || fullName.Length > ValidationConstants.FullNameMaxLength)
            {
                throw new ValidationException($"Full name length must be between {ValidationConstants.FullNameMinLength} and {ValidationConstants.FullNameMaxLength} characters.");
            }
            return true;
        }

        private bool ValidateUsername(string username)
        {
            if (username.Length < ValidationConstants.UsernameMinLength || username.Length > ValidationConstants.UsernameMaxLength)
            {
                throw new ValidationException($"Username length must be between {ValidationConstants.UsernameMinLength} and {ValidationConstants.UsernameMaxLength} characters.");
            }
            return true;
        }

        private bool ValidateEmail(string email)
        {
            Regex regex = new(
                @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!regex.IsMatch(email))
                throw new ValidationException("Email must be a valid format (e.g. example@domain.com).");
            return true;
        }

        public async Task UserChangePassword(Guid id, ChangePasswordRequest request)
        {
            var userAccount = await userRepository.FindUserByIdAsync(id);
            if(userAccount == null)
            {
                throw new KeyNotFoundException("Account with id: " + id);
            }
            if(request.NewPassword != request.ConfirmPassword)
            {
                throw new BusinessException("Confirm password do not match with new password");
            }
            if(!BCrypt.Net.BCrypt.Verify(request.OldPassword, userAccount.PasswordHash))
            {
                throw new ValidationException("Wrong password, please check again");
            }
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await userRepository.UpdatePassword(passwordHash, userAccount);
        }
    }
}
