using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Models;
using SonghaiHMO.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly ActivityLogger _activityLogger;
        private readonly TwoFactorService _twoFactorService;

        public AdminController(ApplicationDbContext context, EmailService emailService, ActivityLogger activityLogger, TwoFactorService twoFactorService)
        {
            _context = context;
            _emailService = emailService;
            _activityLogger = activityLogger;
            _twoFactorService = twoFactorService;
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(a => a.Username == username && a.Password == password);

            if (admin != null)
            {
                // Check if 2FA is enabled
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == admin.Id.ToString());
                if (profile != null && profile.IsTwoFactorEnabled)
                {
                    return Json(new { 
                        requiresTwoFactor = true, 
                        userId = admin.Id.ToString(), 
                        userType = "Admin", 
                        email = admin.Email 
                    });
                }

                HttpContext.Session.SetString("AdminId", admin.Id.ToString());
                HttpContext.Session.SetString("AdminName", admin.FullName);
                HttpContext.Session.SetString("AdminRole", admin.Role);
                HttpContext.Session.SetString("AdminEmail", admin.Email);

                await _activityLogger.LogLogin(admin.Id.ToString(), admin.FullName, admin.Role);

                return Json(new { success = true, redirectUrl = "/Admin/Dashboard" });
            }

            return Json(new { success = false, message = "Invalid credentials" });
        }

        public async Task<IActionResult> Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return RedirectToAction("Login", "Home", new { role = "admin" });

            var totalEnrollees = await _context.Enrollees.CountAsync();
            var totalClaims = await _context.Claims.CountAsync();
            var pendingClaims = await _context.Claims.CountAsync(c => c.Status == "Pending");
            var approvedClaims = await _context.Claims.CountAsync(c => c.Status == "Approved");
            var rejectedClaims = await _context.Claims.CountAsync(c => c.Status == "Rejected");
            var totalPayout = await _context.Claims.Where(c => c.Status == "Approved").SumAsync(c => c.Amount);
            
            var recentClaims = await _context.Claims
                .Include(c => c.Enrollee)
                .OrderByDescending(c => c.SubmittedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.TotalEnrollees = totalEnrollees;
            ViewBag.TotalClaims = totalClaims;
            ViewBag.PendingClaims = pendingClaims;
            ViewBag.ApprovedClaims = approvedClaims;
            ViewBag.RejectedClaims = rejectedClaims;
            ViewBag.TotalPayout = totalPayout;
            ViewBag.RecentClaims = recentClaims;
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            return View();
        }

        public async Task<IActionResult> Enrollees()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return RedirectToAction("Login", "Home", new { role = "admin" });

            var enrollees = await _context.Enrollees.OrderByDescending(e => e.CreatedAt).ToListAsync();
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            return View(enrollees);
        }

        [HttpPost]
        public async Task<IActionResult> AddEnrollee([FromBody] Enrollee enrollee)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return Json(new { success = false, message = "Not authenticated" });

            enrollee.CreatedAt = DateTime.Now;
            _context.Enrollees.Add(enrollee);
            await _context.SaveChangesAsync();

            await _activityLogger.LogEnrolleeAction(
                HttpContext.Session.GetString("AdminId"),
                HttpContext.Session.GetString("AdminName"),
                "Admin",
                enrollee.Id,
                "ADD_ENROLLEE",
                $"Added enrollee: {enrollee.FirstName} {enrollee.LastName}"
            );

            return Json(new { success = true, message = "Enrollee added successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEnrollee(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return Json(new { success = false });

            var enrollee = await _context.Enrollees.FindAsync(id);
            if (enrollee != null)
            {
                var name = enrollee.FullName;
                _context.Enrollees.Remove(enrollee);
                await _context.SaveChangesAsync();

                await _activityLogger.LogEnrolleeAction(
                    HttpContext.Session.GetString("AdminId"),
                    HttpContext.Session.GetString("AdminName"),
                    "Admin",
                    id,
                    "DELETE_ENROLLEE",
                    $"Deleted enrollee: {name}"
                );

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public async Task<IActionResult> Providers()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return RedirectToAction("Login", "Home", new { role = "admin" });

            var providers = await _context.Providers.OrderByDescending(p => p.CreatedAt).ToListAsync();
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            return View(providers);
        }

        [HttpPost]
        public async Task<IActionResult> AddProvider([FromBody] Provider provider)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return Json(new { success = false, message = "Not authenticated" });

            provider.CreatedAt = DateTime.Now;
            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();

            await _emailService.SendWelcomeEmail(provider.Email, provider.ProviderName, "Provider");

            await _activityLogger.LogProviderAction(
                HttpContext.Session.GetString("AdminId"),
                HttpContext.Session.GetString("AdminName"),
                "Admin",
                provider.Id,
                "ADD_PROVIDER",
                $"Added provider: {provider.ProviderName}"
            );

            return Json(new { success = true, message = "Provider added successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProvider([FromBody] Provider provider)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return Json(new { success = false });

            var existing = await _context.Providers.FindAsync(provider.Id);
            if (existing != null)
            {
                var oldName = existing.ProviderName;
                existing.ProviderName = provider.ProviderName;
                existing.Email = provider.Email;
                existing.Phone = provider.Phone;
                existing.Address = provider.Address;
                await _context.SaveChangesAsync();

                await _activityLogger.LogProviderAction(
                    HttpContext.Session.GetString("AdminId"),
                    HttpContext.Session.GetString("AdminName"),
                    "Admin",
                    provider.Id,
                    "UPDATE_PROVIDER",
                    $"Updated provider from '{oldName}' to '{provider.ProviderName}'"
                );

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProvider(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return Json(new { success = false });

            var provider = await _context.Providers.FindAsync(id);
            if (provider != null)
            {
                var name = provider.ProviderName;
                _context.Providers.Remove(provider);
                await _context.SaveChangesAsync();

                await _activityLogger.LogProviderAction(
                    HttpContext.Session.GetString("AdminId"),
                    HttpContext.Session.GetString("AdminName"),
                    "Admin",
                    id,
                    "DELETE_PROVIDER",
                    $"Deleted provider: {name}"
                );

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public async Task<IActionResult> Claims()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return RedirectToAction("Login", "Home", new { role = "admin" });

            var claims = await _context.Claims
                .Include(c => c.Enrollee)
                .Include(c => c.Provider)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();
            
            var providers = await _context.Providers.ToListAsync();
            var enrollees = await _context.Enrollees.ToListAsync();
            
            ViewBag.Providers = providers;
            ViewBag.Enrollees = enrollees;
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            return View(claims);
        }

        [HttpPost]
        public async Task<IActionResult> AddClaim([FromBody] Claim claim)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return Json(new { success = false });

            claim.Status = "Pending";
            claim.SubmittedAt = DateTime.Now;
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            await _activityLogger.LogClaimAction(
                HttpContext.Session.GetString("AdminId"),
                HttpContext.Session.GetString("AdminName"),
                "Admin",
                claim.Id,
                "ADD_CLAIM",
                $"Added claim for enrollee #{claim.EnrolleeId}, amount: ₦{claim.Amount:N0}"
            );

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateClaimStatus(int id, string status, string notes)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return Json(new { success = false });

            var claim = await _context.Claims
                .Include(c => c.Provider)
                .Include(c => c.Enrollee)
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (claim != null)
            {
                var oldStatus = claim.Status;
                claim.Status = status;
                claim.AdminNotes = notes ?? "";
                await _context.SaveChangesAsync();

                // Send email notification
                await _emailService.SendClaimStatusEmail(
                    claim.Provider.Email,
                    claim.Provider.ProviderName,
                    claim.Id,
                    status,
                    notes,
                    claim.Enrollee.FullName,
                    claim.Amount
                );

                // Log activity
                await _activityLogger.LogClaimAction(
                    HttpContext.Session.GetString("AdminId"),
                    HttpContext.Session.GetString("AdminName"),
                    "Admin",
                    claim.Id,
                    $"UPDATE_CLAIM_STATUS_{status}",
                    $"Claim status changed from {oldStatus} to {status}. Notes: {notes}"
                );

                return Json(new { success = true, emailSent = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> BulkApproveClaims([FromBody] int[] claimIds)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return Json(new { success = false });

            var approvedCount = 0;
            foreach (var id in claimIds)
            {
                var claim = await _context.Claims
                    .Include(c => c.Provider)
                    .Include(c => c.Enrollee)
                    .FirstOrDefaultAsync(c => c.Id == id);
                    
                if (claim != null && claim.Status == "Pending")
                {
                    claim.Status = "Approved";
                    claim.AdminNotes = "Bulk approved by admin";
                    approvedCount++;

                    await _emailService.SendClaimStatusEmail(
                        claim.Provider.Email,
                        claim.Provider.ProviderName,
                        claim.Id,
                        "Approved",
                        "Bulk approval",
                        claim.Enrollee.FullName,
                        claim.Amount
                    );
                }
            }
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                HttpContext.Session.GetString("AdminId"),
                HttpContext.Session.GetString("AdminName"),
                "Admin",
                "BULK_APPROVE_CLAIMS",
                "Claim",
                string.Join(",", claimIds),
                $"Bulk approved {approvedCount} claims"
            );

            return Json(new { success = true, count = approvedCount });
        }

        public async Task<IActionResult> Reports(string startDate, string endDate, string status, int? providerId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return RedirectToAction("Login", "Home", new { role = "admin" });

            var query = _context.Claims
                .Include(c => c.Enrollee)
                .Include(c => c.Provider)
                .AsQueryable();

            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                var start = DateTime.Parse(startDate);
                var end = DateTime.Parse(endDate);
                query = query.Where(c => c.SubmittedAt.Date >= start.Date && c.SubmittedAt.Date <= end.Date);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(c => c.Status == status);

            if (providerId.HasValue && providerId.Value > 0)
                query = query.Where(c => c.ProviderId == providerId.Value);

            var claims = await query.OrderByDescending(c => c.SubmittedAt).ToListAsync();
            var providers = await _context.Providers.ToListAsync();

            ViewBag.Providers = providers;
            ViewBag.FilteredClaims = claims;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.StatusFilter = status;
            ViewBag.SelectedProvider = providerId;
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _activityLogger.LogLogout(
                HttpContext.Session.GetString("AdminId"),
                HttpContext.Session.GetString("AdminName"),
                "Admin"
            );
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home", new { role = "admin" });
        }
    }
}