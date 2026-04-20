using SonghaiHMO.Data;
using SonghaiHMO.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonghaiHMO.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddNotification(string userId, string userType, string title, string message, string type = "info", string linkUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId ?? "",
                UserType = userType ?? "",
                Title = title ?? "",
                Message = message ?? "",
                Type = type ?? "info",
                LinkUrl = linkUrl ?? "",
                IsRead = false,
                CreatedAt = System.DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public List<Notification> GetUnreadNotifications(string userId)
        {
            return _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToList();
        }

        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}