using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Models;
using SonghaiHMO.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TwoFactorService _twoFactorService;
        private readonly EmailService _emailService;

        public ProfileController(ApplicationDbContext context, TwoFactorService twoFactorService, EmailService emailService)
        {
            _context = context;
            _twoFactorService = twoFactorService;
            _emailService = emailService;
        }

        private string GetCurrentUserId()
        {
            return HttpContext.Session.GetString("AdminId") 
                ?? HttpContext.Session.GetString("ProviderId") 
                ?? HttpContext.Session.GetString("EnrolleeId");
        }

        private string GetCurrentUserType()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId"))) return "Admin";
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("ProviderId"))) return "Provider";
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("EnrolleeId"))) return "Enrollee";
            return "Unknown";
        }

        private string GetCurrentUserName()
        {
            return HttpContext.Session.GetString("AdminName") 
                ?? HttpContext.Session.GetString("ProviderName") 
                ?? HttpContext.Session.GetString("EnrolleeName");
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = userId,
                    UserType = GetCurrentUserType(),
                    FullName = GetCurrentUserName(),
                    Email = HttpContext.Session.GetString("AdminEmail") ?? HttpContext.Session.GetString("ProviderEmail"),
                    IsTwoFactorEnabled = false
                };
                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            ViewBag.UserName = GetCurrentUserName();
            ViewBag.UserType = GetCurrentUserType();
            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfile model)
        {
            var userId = GetCurrentUserId();
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            
            if (profile != null)
            {
                profile.FullName = model.FullName;
                profile.Phone = model.Phone;
                profile.Address = model.Address;
                profile.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Update session name
                HttpContext.Session.SetString("AdminName", model.FullName);
                HttpContext.Session.SetString("ProviderName", model.FullName);
                HttpContext.Session.SetString("EnrolleeName", model.FullName);

                return Json(new { success = true, message = "Profile updated successfully" });
            }
            return Json(new { success = false, message = "Profile not found" });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = GetCurrentUserId();
            var userType = GetCurrentUserType();

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "New passwords do not match" });

            if (userType == "Admin")
            {
                var admin = await _context.AdminUsers.FindAsync(int.Parse(userId));
                if (admin != null && admin.Password == currentPassword)
                {
                    admin.Password = newPassword;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Password changed successfully" });
                }
            }
            else if (userType == "Provider")
            {
                var provider = await _context.Providers.FindAsync(int.Parse(userId));
                if (provider != null && provider.Password == currentPassword)
                {
                    provider.Password = newPassword;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Password changed successfully" });
                }
            }

            return Json(new { success = false, message = "Current password is incorrect" });
        }

        [HttpPost]
        public async Task<IActionResult> EnableTwoFactor()
        {
            var userId = GetCurrentUserId();
            await _twoFactorService.EnableTwoFactor(userId);
            return Json(new { success = true, message = "Two-factor authentication enabled" });
        }

        [HttpPost]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var userId = GetCurrentUserId();
            await _twoFactorService.DisableTwoFactor(userId);
            return Json(new { success = true, message = "Two-factor authentication disabled" });
        }

        [HttpGet]
        public async Task<IActionResult> TwoFactorStatus()
        {
            var userId = GetCurrentUserId();
            var isEnabled = await _twoFactorService.IsTwoFactorEnabled(userId);
            return Json(new { enabled = isEnabled });
        }

        [HttpPost]
        public async Task<IActionResult> SendVerificationCode()
        {
            var userId = GetCurrentUserId();
            var email = HttpContext.Session.GetString("AdminEmail") ?? HttpContext.Session.GetString("ProviderEmail");
            
            if (string.IsNullOrEmpty(email))
            {
                var admin = await _context.AdminUsers.FindAsync(int.Parse(userId));
                email = admin?.Email;
            }

            await _twoFactorService.SendTwoFactorCode(userId, email);
            return Json(new { success = true, message = "Verification code sent to your email" });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyAndEnable(string code)
        {
            var userId = GetCurrentUserId();
            var isValid = await _twoFactorService.VerifyCode(userId, code);
            
            if (isValid)
            {
                await _twoFactorService.EnableTwoFactor(userId);
                return Json(new { success = true, message = "Two-factor authentication enabled successfully" });
            }
            return Json(new { success = false, message = "Invalid or expired code" });
        }
    }
}