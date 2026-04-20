using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Models;
using SonghaiHMO.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class ProviderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TwoFactorService _twoFactorService;

        public ProviderController(ApplicationDbContext context, TwoFactorService twoFactorService)
        {
            _context = context;
            _twoFactorService = twoFactorService;
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Email == email && p.Password == password);

            if (provider != null)
            {
                // Check if 2FA is enabled
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == provider.Id.ToString());
                if (profile != null && profile.IsTwoFactorEnabled)
                {
                    return Json(new
                    {
                        requiresTwoFactor = true,
                        userId = provider.Id.ToString(),
                        userType = "Provider",
                        email = provider.Email
                    });
                }

                HttpContext.Session.SetString("ProviderId", provider.Id.ToString());
                HttpContext.Session.SetString("ProviderName", provider.ProviderName);
                HttpContext.Session.SetString("ProviderEmail", provider.Email);

                return Json(new { success = true, redirectUrl = "/Provider/Dashboard" });
            }

            return Json(new { success = false, message = "Invalid credentials" });
        }

        public async Task<IActionResult> Dashboard()
        {
            var providerId = HttpContext.Session.GetString("ProviderId");
            if (string.IsNullOrEmpty(providerId))
                return RedirectToAction("Login", "Home", new { role = "provider" });

            var providerName = HttpContext.Session.GetString("ProviderName");
            
            var claims = await _context.Claims
                .Include(c => c.Enrollee)
                .Where(c => c.ProviderId == int.Parse(providerId))
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();

            var totalClaims = claims.Count;
            var pendingClaims = claims.Count(c => c.Status == "Pending");
            var approvedClaims = claims.Count(c => c.Status == "Approved");
            var rejectedClaims = claims.Count(c => c.Status == "Rejected");
            var totalAmount = claims.Where(c => c.Status == "Approved").Sum(c => c.Amount);

            ViewBag.ProviderName = providerName;
            ViewBag.TotalClaims = totalClaims;
            ViewBag.PendingClaims = pendingClaims;
            ViewBag.ApprovedClaims = approvedClaims;
            ViewBag.RejectedClaims = rejectedClaims;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.Claims = claims;
            
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home", new { role = "provider" });
        }
    }
}