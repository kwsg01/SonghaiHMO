using System;

namespace SonghaiHMO.Models
{
    public class EnrolleeUser
    {
        public int Id { get; set; }
        public int EnrolleeId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PasswordSalt { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public virtual Enrollee? Enrollee { get; set; }
    }
}