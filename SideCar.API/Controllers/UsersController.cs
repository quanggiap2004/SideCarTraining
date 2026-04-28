using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SideCar.Business.DTOs;
using SideCar.Business.DTOs.Params;
using SideCar.Business.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SideCar.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(IUserService userService) : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUserAccounts([FromQuery] QueryUserParams userParams)
        {
            var (items, totalCount) = await userService.GetAllUserAsync(userParams);
            var pagination = new Pagination<UserResponseDto>(items.ToList(), totalCount, userParams.Page, userParams.PageSize);
            return Ok(new BaseResponse<Pagination<UserResponseDto>>("Users retrieved successfully", pagination));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccountAsync(Guid id, UpdateAccountDto updateAccountDto)
        {
            updateAccountDto.Id = id;
            var result = await userService.UpdateUserAccount(updateAccountDto);
            return result
                ? Ok(new BaseResponse<object>("Account updated successfully", null))
                : BadRequest(new BaseResponse<object>("Update failed", null));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteAccount(Guid id)
        {
            var result = await userService.DeleteUserAccount(id);
            return result
                ? Ok(new BaseResponse<object>("Account deleted successfully", null))
                : BadRequest(new BaseResponse<object>("Account already deleted", null));
        }

        [Authorize]
        [HttpGet("{id}/profile")]
        public async Task<IActionResult> GetProfile(Guid id)
        {
            var requestedId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if(requestedId != id)
            {
                throw new ValidationException("You can only view your own profile");
            }
            var profile = await userService.GetUserProfileDtoAsync(requestedId);
            return Ok(new BaseResponse<UserProfileDto>("Profile retrieved successfully", profile));
        }

        [Authorize]
        [HttpPut("{id}/profile")]
        public async Task<IActionResult> UpdateProfile(Guid id, UpdateUserProfileDto request)
        {
            var requestedId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if(requestedId != id)
            {
                throw new ValidationException("You can only update your own profile");
            }
            request.Id = id;
            await userService.UpdateUserProfile(request);
            return Ok(new BaseResponse<bool>("Profile updated successfully", true));
        }

        [Authorize]
        [HttpPut("{id}/profile/password")]
        public async Task<IActionResult> ChangePassword(Guid id, ChangePasswordRequest request)
        {
            var requestedId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (requestedId != id)
            {
                throw new ValidationException("You can only update your own profile");
            }
            await userService.UserChangePassword(id, request);
            return Ok(new BaseResponse<bool>("Profile updated successfully", true));
        }
    }
}
