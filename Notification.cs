using System;

namespace SonghaiHMO.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public string? LinkUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}