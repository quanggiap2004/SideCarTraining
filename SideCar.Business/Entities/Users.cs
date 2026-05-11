using SideCar.Business.Enums;

namespace SideCar.Business.Entities
{
    public class Users : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public Roles Role { get; set; } = Roles.User;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? RefreshTokenExpiry { get; set; }
        public bool IsDeleted { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordExpiry { get; set; }
        public AccountStatus Status { get; set; } = AccountStatus.Active;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? WarningSentAt { get; set; }
        public ICollection<UserActivityLog>? ActivityLog { get; set; }
    }
}
