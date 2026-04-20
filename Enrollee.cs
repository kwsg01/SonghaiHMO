using System;

namespace SonghaiHMO.Models
{
    public class Enrollee
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string AlternativePhone { get; set; } = string.Empty;
        public string PlanType { get; set; } = "Basic";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string FullName => $"{FirstName} {LastName}";
    }
}