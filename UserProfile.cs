using System;

namespace SonghaiHMO.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? ProfilePicture { get; set; }
        public string? TwoFactorSecret { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
        public DateTime? LastPasswordChange { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}