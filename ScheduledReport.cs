using System;

namespace SonghaiHMO.Models
{
    public class ScheduledReport
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string RecipientEmails { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime LastSent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}