using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SideCar.Business.DTOs;
using SideCar.Business.Entities;
using SideCar.Business.Enums;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SideCar.Business.Services
{
    public class AuthenService(IConfiguration config, IUnitOfWork _unitOfWork, IEmailPublisher emailPublisher) : IAuthenService
    {
        public async Task<LoginResponse?> LoginAsync(string username, string password)
        {
            var user = await _unitOfWork.Authen.GetByUsernameAsync(username);
            if (user is null || user.IsDeleted || user.Status == AccountStatus.Deactivated)
            {
                return null;
            }
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                await _unitOfWork.ActivityLogs.AddAsync(new UserActivityLog
                {
                    ActivityType = ActivityType.LoginFailed,
                    CreatedAt = DateTime.UtcNow,
                    UserId = user.Id,
                });
                await _unitOfWork.CommitAsync();
                return null;
            }
            user.LastLoginAt = DateTime.UtcNow;
            user.WarningSentAt = null;
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = HashToken(refreshToken);
            user.RefreshTokenExpiry = DateTime.UtcNow.AddMinutes(
                int.Parse(config["Jwt:RefreshExpiryMinutes"]!));

            await _unitOfWork.ActivityLogs.AddAsync(new UserActivityLog
            {
                ActivityType = ActivityType.Login,
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
            });

            await _unitOfWork.CommitAsync();

            return new LoginResponse
            {
                AccessToken = GenerateJwt(user),
                RefreshToken = refreshToken,
            };
        }

        public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
        {
            var hashedToken = HashToken(refreshToken);
            var user = await _unitOfWork.Authen.GetByRefreshTokenAsync(hashedToken);

            if (user is null)
                return null;

            if (user.RefreshTokenExpiry is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                user.RefreshToken = string.Empty;
                await _unitOfWork.CommitAsync();
                return null;
            }

            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = HashToken(newRefreshToken);
            user.RefreshTokenExpiry = DateTime.UtcNow.AddMinutes(
                int.Parse(config["Jwt:RefreshExpiryMinutes"]!));

            await _unitOfWork.CommitAsync();

            return new LoginResponse
            {
                AccessToken = GenerateJwt(user),
                RefreshToken = newRefreshToken,
            };
        }

        public Task<bool> RegisterAsync(RegisterRequest request) => CreateUserAsync(request, Roles.User);

        public Task<bool> RegisterAdminAsync(RegisterRequest request) => CreateUserAsync(request, Roles.Admin);

        private async Task<bool> CreateUserAsync(RegisterRequest request, Roles role)
        {
            var exists = await _unitOfWork.Authen.ExistsAsync(request.Username, request.Email, request.PhoneNumber);
            if (exists)
            {
                Console.WriteLine($"[sidecar-authen] Register failed — '{request.Username}' already exists.");
                return false;
            }

            var newUser = new Users
            {
                Username = request.Username,
                Email = request.Email,
                Fullname = request.FullName,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = role
            };

            await _unitOfWork.Authen.AddUserAsync(newUser);
            await _unitOfWork.CommitAsync();

            if (role == Roles.User)
            {
                emailPublisher.QueueTemplateEmail(new TemplateEmailRequest
                {
                    Email = request.Email,
                    Subject = "Welcome to our system",
                    TemplateName = "welcome",
                    Placeholders = new Dictionary<string, string>
                    {
                        { "FullName", newUser.Fullname },
                        { "Email", newUser.Email },
                        { "Username", newUser.Username }
                    }
                });
            }

            return true;
        }

        public TokenValidationResult? ValidateToken(string token)
        {
            var secret = config["Jwt:Secret"]!;
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            try
            {
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var username = principal.FindFirstValue(ClaimTypes.Name)!;
                var role = principal.FindFirstValue(ClaimTypes.Role)!;
                Console.WriteLine($"[sidecar-authen] Token validated for '{username}' at {DateTime.UtcNow:HH:mm:ss}");
                return new TokenValidationResult(userId, username, role);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[sidecar-authen] Token validation failed: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _unitOfWork.Authen.FindByAsync(x => x.Email == request.Email);
            if (user is null)
                throw new KeyNotFoundException("User with email: " + request.Email + " not found");

            var rawToken = GenerateRefreshToken();
            user.ResetPasswordToken = HashToken(rawToken);
            user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(15);

            await _unitOfWork.CommitAsync();

            var resetLink = $"{config["BaseUrl:Url"]}/auth/reset-password?token={Uri.EscapeDataString(rawToken)}";

            emailPublisher.QueueTemplateEmail(new TemplateEmailRequest
            {
                Email = user.Email,
                Subject = "Reset your password",
                TemplateName = "reset-password",
                Placeholders = new Dictionary<string, string>
                {
                    { "FullName", user.Fullname },
                    { "ResetLink", resetLink }
                }
            });

            return rawToken;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var hashedToken = HashToken(token);
            var user = await _unitOfWork.Authen.FindByAsync(u => u.ResetPasswordToken == hashedToken);

            if (user is null || user.ResetPasswordExpiry is null || user.ResetPasswordExpiry < DateTime.UtcNow)
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;

            var userActivity = new UserActivityLog
            {
                ActivityType = ActivityType.ResetPassword,
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id
            };
            await _unitOfWork.ActivityLogs.AddAsync(userActivity);
            await _unitOfWork.CommitAsync();
            return true;
        }

        private string GenerateJwt(Users user)
        {
            var secret = config["Jwt:Secret"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiry),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
