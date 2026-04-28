using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SideCar.Authen.DTOs;
using SideCar.Business.DTOs;
using SideCar.Business.Entities;
using SideCar.Business.Enums;
using SideCar.Business.Repositories;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SideCar.Authen
{
    public class AuthenService(IConfiguration config, IAuthenRepository authenRepository, IEmailPublisher emailPublisher) : IAuthenService
    {
        public async Task<LoginResponse?> LoginAsync(string username, string password)
        {
            var user = await authenRepository.GetByUsernameAsync(username);
            if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                Console.WriteLine($"[sidecar-authen] Login failed for '{username}' at {DateTime.UtcNow:HH:mm:ss}");
                return null;
            }

            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = HashToken(refreshToken);
            user.RefreshTokenExpiry = DateTime.UtcNow.AddMinutes(
                int.Parse(config["Jwt:RefreshExpiryMinutes"] ?? "180"));

            await authenRepository.SaveChangesAsync();

            return new LoginResponse
            {
                AccessToken  = GenerateJwt(user),
                RefreshToken = refreshToken,
            };
        }

        public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
        {
            var hashedToken = HashToken(refreshToken);
            var user = await authenRepository.GetByRefreshTokenAsync(hashedToken);

            if (user is null)
            {
                return null;
            }

            if (user.RefreshTokenExpiry is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                user.RefreshToken = string.Empty;
                await authenRepository.SaveChangesAsync();
                return null;
            }

            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = HashToken(newRefreshToken);
            user.RefreshTokenExpiry = DateTime.UtcNow.AddMinutes(
                int.Parse(config["Jwt:RefreshExpiryMinutes"]!));

            await authenRepository.SaveChangesAsync();

            return new LoginResponse
            {
                AccessToken  = GenerateJwt(user),
                RefreshToken = newRefreshToken,
            };
        }

        public Task<bool> RegisterAsync(RegisterRequest request)
            => CreateUserAsync(request, Roles.User);

        public Task<bool> RegisterAdminAsync(RegisterRequest request)
            => CreateUserAsync(request, Roles.Admin);

        private async Task<bool> CreateUserAsync(RegisterRequest request, Roles role)
        {
            var exists = await authenRepository.ExistsAsync(request.Username, request.Email, request.PhoneNumber);
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

            await authenRepository.AddUserAsync(newUser);

            if (role == Roles.User)
            {
                var templateRequest = new TemplateEmailRequest
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
                };
                emailPublisher.QueueTemplateEmail(templateRequest);
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

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public async Task<string?> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await authenRepository.FindByAsync(x => x.Email == request.Email);
            if (user is null)
            {
                throw new KeyNotFoundException("User with email: " + request.Email + " not found");
            }

            var rawToken = GenerateRefreshToken();
            user.ResetPasswordToken = HashToken(rawToken);
            user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(15);

            await authenRepository.SaveChangesAsync();

            var resetLink = $"{config["BaseUrl:Url"]}/auth/reset-password?token={Uri.EscapeDataString(rawToken)}";

            var templateRequest = new TemplateEmailRequest
            {
                Email = user.Email,
                Subject = "Reset your password",
                TemplateName = "reset-password",
                Placeholders = new Dictionary<string, string>
                {
                    { "FullName",  user.Fullname },
                    { "ResetLink", resetLink }
                }
            };
            emailPublisher.QueueTemplateEmail(templateRequest);

            return rawToken;
        }
        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var hashedToken = HashToken(token);
            var user = await authenRepository.FindByAsync(u => u.ResetPasswordToken == hashedToken);

            if (user is null || user.ResetPasswordExpiry is null || user.ResetPasswordExpiry < DateTime.UtcNow)
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;

            await authenRepository.SaveChangesAsync();
            return true;
        }
    }
}
