using Microsoft.AspNetCore.Mvc;
using SideCar.Authen.DTOs;
using SideCar.Business.DTOs;

namespace SideCar.Authen.Controllers
{
    [ApiController]
    [Route("/auth")]
    public class AuthController(IAuthenService authenService) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var token = await authenService.LoginAsync(req.Username, req.Password);
            return token is null ? Unauthorized() : Ok(new { token });
        }

        [HttpPost("validate")]
        public IActionResult Validate([FromBody] ValidateRequest req)
        {
            var result = authenService.ValidateToken(req.Token);
            return result is null
                ? Unauthorized()
                : Ok(new { result.UserId, result.Username, result.Role });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var success = await authenService.RegisterAsync(req);
            return success
                ? Ok(new { Message = "User registered successfully." })
                : Conflict(new { Message = "Register failed" });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            var result = await authenService.RefreshTokenAsync(req.RefreshToken);
            return result is null ? Unauthorized() : Ok(new { result });
        }

        [HttpPost("admin/register")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterRequest req)
        {
            var success = await authenService.RegisterAdminAsync(req);
            return success
                ? Ok(new { Message = "Admin registered successfully." })
                : Conflict(new { Message = "Register failed" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            var token = await authenService.ForgotPasswordAsync(req);
            return token is null
                ? NotFound(new { Message = "No account found with that email." })
                : Ok(new { Message = "Reset token generated.", ResetToken = token });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var success = await authenService.ResetPasswordAsync(req.Token, req.NewPassword);
            return success
                ? Ok(new { Message = "Password reset successfully." })
                : BadRequest(new { Message = "Invalid or expired reset token." });
        }
    }
}
