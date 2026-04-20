using SonghaiHMO.Data;
using SonghaiHMO.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace SonghaiHMO.Services
{
    public class ActivityLogger
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogger(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string userId, string userName, string userRole, string action, string entityType, string entityId, string details = "")
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";

            var log = new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.Now
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogLogin(string userId, string userName, string userRole)
        {
            await LogAsync(userId, userName, userRole, "LOGIN", "User", userId, "User logged in successfully");
        }

        public async Task LogLogout(string userId, string userName, string userRole)
        {
            await LogAsync(userId, userName, userRole, "LOGOUT", "User", userId, "User logged out");
        }

        public async Task LogClaimAction(string userId, string userName, string userRole, int claimId, string action, string details)
        {
            await LogAsync(userId, userName, userRole, action, "Claim", claimId.ToString(), details);
        }

        public async Task LogEnrolleeAction(string userId, string userName, string userRole, int enrolleeId, string action, string details)
        {
            await LogAsync(userId, userName, userRole, action, "Enrollee", enrolleeId.ToString(), details);
        }

        public async Task LogProviderAction(string userId, string userName, string userRole, int providerId, string action, string details)
        {
            await LogAsync(userId, userName, userRole, action, "Provider", providerId.ToString(), details);
        }
    }
}