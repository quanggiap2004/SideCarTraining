using AutoMapper;
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
    public class UserService(IUnitOfWork _unitOfWork, IEmailPublisher _emailPublisher, IMapper _mapper, IUserActivityLogRepository _activityLogRepository) : IUserService
    {
        public async Task<bool> DeleteUserAccount(Guid id)
        {
            var userAccount = await _unitOfWork.Users.FindUserByIdAsync(id);
            if (userAccount == null)
                throw new KeyNotFoundException("Account with id:" + id);

            if (userAccount.IsDeleted)
                return false;

            if (userAccount.Role == Roles.Admin)
                throw new BusinessException("Cannot delete admin account");

            userAccount.IsDeleted = true;
            await _unitOfWork.CommitAsync();

            _emailPublisher.QueueTemplateEmail(new TemplateEmailRequest
            {
                Email = userAccount.Email,
                Subject = "Account deactivation",
                TemplateName = "account-deleted",
                Placeholders = new Dictionary<string, string>
                {
                    { "FullName", userAccount.Fullname },
                    { "Email", userAccount.Email },
                    { "Username", userAccount.Username },
                    { "DeletedAt", DateTime.UtcNow.ToString("dd MMM yyyy, HH:mm UTC") }
                }
            });

            return true;
        }

        public async Task<(IEnumerable<UserResponseDto>, int totalCount)> GetAllUserAsync(QueryUserParams userParams)
        {
            var userList = await _unitOfWork.Users.GetPagedUsersAsync(userParams);
            var totalCount = await _unitOfWork.Users.CountUsersAsync(userParams);
            return (_mapper.Map<List<UserResponseDto>>(userList), totalCount);
        }

        public async Task<UserProfileDto> GetUserProfileDtoAsync(Guid id)
        {
            var userAccount = await _unitOfWork.Users.FindUserByIdAsync(id);
            if (userAccount == null)
                throw new ArgumentException("Cannot find account with id: " + id);

            return _mapper.Map<UserProfileDto>(userAccount);
        }

        public async Task<bool> UpdateUserAccount(UpdateAccountDto account)
        {
            if (account.Id == null)
                throw new KeyNotFoundException("Id is null");

            var updateUser = await _unitOfWork.Users.FindUserByIdAsync(account.Id.Value);
            if (updateUser == null)
                throw new KeyNotFoundException("Account with id:" + account.Id);

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
            if(account.Status != null)
            {
                updateUser.Status = account.Status.Value;
                if(account.Status == AccountStatus.Active)
                {
                    var currentTime = DateTime.UtcNow;
                    updateUser.LastLoginAt = currentTime;
                    updateUser.WarningSentAt = null;
                    await _activityLogRepository.AddAsync(new UserActivityLog
                    {
                        UserId = updateUser.Id,
                        ActivityType = ActivityType.Login,
                        CreatedAt = currentTime
                    }); // Log user login activity when account is re-activated by admin
                }
            }

            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task UpdateUserProfile(UpdateUserProfileDto account)
        {
            var accountUpdate = await _unitOfWork.Users.FindUserByIdAsync(account.Id.Value);
            if (accountUpdate == null)
                throw new KeyNotFoundException("Account with id: " + account.Id + " not found");

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
            if (account.Email != null)
            {
                ValidateEmail(account.Email);
                accountUpdate.Email = account.Email;
            }

            await _unitOfWork.CommitAsync();
        }

        public async Task UserChangePassword(Guid id, ChangePasswordRequest request)
        {
            var userAccount = await _unitOfWork.Users.FindUserByIdAsync(id);
            if (userAccount == null)
                throw new KeyNotFoundException("Account with id: " + id);

            if (request.NewPassword != request.ConfirmPassword)
                throw new BusinessException("Confirm password do not match with new password");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, userAccount.PasswordHash))
                throw new ValidationException("Wrong password, please check again");

            _unitOfWork.Users.UpdatePassword(BCrypt.Net.BCrypt.HashPassword(request.NewPassword), userAccount);
            await _unitOfWork.CommitAsync();
        }

        private static bool ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ValidationException("Phone number is required.");
            if (!phone.All(char.IsDigit) || phone.Length != ProjectConstant.PhoneLength)
                throw new ValidationException($"Phone number must be exactly {ProjectConstant.PhoneLength} digits.");
            return true;
        }

        private static bool ValidateFullName(string fullName)
        {
            if (fullName.Length < ProjectConstant.FullNameMinLength || fullName.Length > ProjectConstant.FullNameMaxLength)
                throw new ValidationException($"Full name length must be between {ProjectConstant.FullNameMinLength} and {ProjectConstant.FullNameMaxLength} characters.");
            return true;
        }

        private static bool ValidateUsername(string username)
        {
            if (username.Length < ProjectConstant.UsernameMinLength || username.Length > ProjectConstant.UsernameMaxLength)
                throw new ValidationException($"Username length must be between {ProjectConstant.UsernameMinLength} and {ProjectConstant.UsernameMaxLength} characters.");
            return true;
        }

        private static bool ValidateEmail(string email)
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!regex.IsMatch(email))
                throw new ValidationException("Email must be a valid format (e.g. example@domain.com).");
            return true;
        }
    }
}
