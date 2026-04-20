using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int days = 7)
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToAction("Login", "Home", new { role = "admin" });

            var cutoffDate = System.DateTime.Now.AddDays(-days);
            var logs = await _context.ActivityLogs
                .Where(l => l.Timestamp >= cutoffDate)
                .OrderByDescending(l => l.Timestamp)
                .Take(200)
                .ToListAsync();

            ViewBag.Days = days;
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(int days = 7)
        {
            var cutoffDate = System.DateTime.Now.AddDays(-days);
            var logs = await _context.ActivityLogs
                .Where(l => l.Timestamp >= cutoffDate)
                .OrderByDescending(l => l.Timestamp)
                .Take(200)
                .ToListAsync();
            return Json(logs);
        }
    }
}