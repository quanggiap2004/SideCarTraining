using SideCar.Business.DTOs;

namespace SideCar.Business
{
    public interface IAuthenService
    {
        Task<LoginResponse?> LoginAsync(string username, string password);
        Task<LoginResponse?> RefreshTokenAsync(string refreshToken);
        TokenValidationResult? ValidateToken(string token);
        Task<bool> RegisterAsync(RegisterRequest request);
        Task<bool> RegisterAdminAsync(RegisterRequest request);
        Task<string?> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }
    public record TokenValidationResult(Guid UserId, string Username, string Role);
}
