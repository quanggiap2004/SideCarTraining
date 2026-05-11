using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideCar.Business.DTOs;
using SideCar.Business.Services.Interfaces;

namespace SideCar.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueuesController(IQueueService queueService) : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpPost("dlq/redrive")]
        public async Task<IActionResult> RedriveDlq()
        {
            var redriven = await queueService.RedriveDlqAsync();
            return Ok(new BaseResponse<int>($"Redriven {redriven} messages", redriven));
        }
    }
}
