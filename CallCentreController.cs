using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Models;
using SonghaiHMO.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class CallCentreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TwoFactorService _twoFactorService;

        public CallCentreController(ApplicationDbContext context, TwoFactorService twoFactorService)
        {
            _context = context;
            _twoFactorService = twoFactorService;
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var staff = await _context.CallCentreStaff
                .FirstOrDefaultAsync(s => s.Email == email && s.Password == password);

            if (staff != null)
            {
                // Check if 2FA is enabled
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == staff.Id.ToString());
                if (profile != null && profile.IsTwoFactorEnabled)
                {
                    return Json(new
                    {
                        requiresTwoFactor = true,
                        userId = staff.Id.ToString(),
                        userType = "CallCentre",
                        email = staff.Email
                    });
                }

                HttpContext.Session.SetString("StaffId", staff.Id.ToString());
                HttpContext.Session.SetString("StaffName", staff.FullName);
                HttpContext.Session.SetString("StaffEmail", staff.Email);

                return Json(new { success = true, redirectUrl = "/CallCentre/Dashboard" });
            }

            return Json(new { success = false, message = "Invalid credentials" });
        }

        public async Task<IActionResult> Dashboard()
        {
            var staffId = HttpContext.Session.GetString("StaffId");
            if (string.IsNullOrEmpty(staffId))
                return RedirectToAction("Login", "Home", new { role = "callcentre" });

            var staffName = HttpContext.Session.GetString("StaffName");
            
            var pendingClaims = await _context.Claims
                .Include(c => c.Enrollee)
                .Include(c => c.Provider)
                .Where(c => c.Status == "Pending")
                .OrderBy(c => c.SubmittedAt)
                .ToListAsync();

            var totalPending = pendingClaims.Count;
            var totalClaims = await _context.Claims.CountAsync();
            var approvedToday = await _context.Claims
                .CountAsync(c => c.Status == "Approved" && c.SubmittedAt.Date == DateTime.Today);

            ViewBag.StaffName = staffName;
            ViewBag.PendingClaims = pendingClaims;
            ViewBag.TotalPending = totalPending;
            ViewBag.TotalClaims = totalClaims;
            ViewBag.ApprovedToday = approvedToday;
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AuthorizeClaim(int id, string status, string notes)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = status;
                claim.AdminNotes = notes ?? "Authorized by call centre";
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home", new { role = "callcentre" });
        }
    }
}