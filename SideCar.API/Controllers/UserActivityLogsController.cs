using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideCar.Business.DTOs;
using SideCar.Business.DTOs.Params;
using SideCar.Business.Services.Interfaces;

namespace SideCar.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserActivityLogsController(IUserActivityLogService userActivityLogService) : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetPagedUserActivityLog([FromQuery] QueryUserActivityLogParams userActivityLogParams)
        {
            var result = await userActivityLogService.GetPagedUserActivityLogAsync(userActivityLogParams);
            return Ok(new BaseResponse<Pagination<UserActivityLogDto>>("Activity logs retrieved successfully", result));
        }
    }
}
