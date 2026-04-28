using SideCar.Business.DTOs;
using SideCar.Business.DTOs.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Services.Interfaces
{
    public interface IUserService
    {
        Task<(IEnumerable<UserResponseDto>, int totalCount)> GetAllUserAsync(QueryUserParams userParams);
        Task<bool> UpdateUserAccount(UpdateAccountDto account);
        Task<bool> DeleteUserAccount(Guid id);
        Task<UserProfileDto> GetUserProfileDtoAsync(Guid id);
        Task UpdateUserProfile(UpdateUserProfileDto account);
        Task UserChangePassword(Guid id, ChangePasswordRequest request);
    }
}
