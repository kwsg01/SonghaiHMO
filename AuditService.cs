using SonghaiHMO.Data;
using SonghaiHMO.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace SonghaiHMO.Services
{
    public class AuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string userName, string userRole, string action, string entityType, string entityId, object oldValues = null, object newValues = null)
        {
            var log = new AuditLog
            {
                UserId = userId ?? "",
                UserName = userName ?? "",
                UserRole = userRole ?? "",
                Action = action ?? "",
                EntityType = entityType ?? "",
                EntityId = entityId ?? "",
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : "",
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : "",
                IpAddress = "127.0.0.1",
                UserAgent = "System",
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}