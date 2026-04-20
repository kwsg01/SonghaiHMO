using System;

namespace SonghaiHMO.Models
{
    public class Claim
    {
        public int Id { get; set; }
        public int EnrolleeId { get; set; }
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public string AdminNotes { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Enrollee? Enrollee { get; set; }
        public virtual Provider? Provider { get; set; }
    }
}