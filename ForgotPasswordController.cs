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
    public class ForgotPasswordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public ForgotPasswordController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendResetLink(string email)
        {
            // Check in AdminUsers
            var admin = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Email == email);
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Email == email);
            var enrolleeUser = await _context.EnrolleeUsers.Include(e => e.Enrollee).FirstOrDefaultAsync(e => e.Username == email);

            string userId = null;
            string userType = null;

            if (admin != null)
            {
                userId = admin.Id.ToString();
                userType = "Admin";
            }
            else if (provider != null)
            {
                userId = provider.Id.ToString();
                userType = "Provider";
            }
            else if (enrolleeUser != null)
            {
                userId = enrolleeUser.Id.ToString();
                userType = "Enrollee";
            }

            if (userId != null)
            {
                var token = Guid.NewGuid().ToString();
                var resetRequest = new PasswordReset
                {
                    UserId = userId,
                    Token = token,
                    ExpiresAt = DateTime.Now.AddHours(1),
                    IsUsed = false
                };

                _context.PasswordResets.Add(resetRequest);
                await _context.SaveChangesAsync();

                var resetLink = Url.Action("Reset", "ForgotPassword", new { token = token, userId = userId, userType = userType }, Request.Scheme);
                await _emailService.SendPasswordResetEmail(email, resetLink);

                ViewBag.Message = "Password reset link has been sent to your email.";
            }
            else
            {
                ViewBag.Message = "If an account exists with this email, you will receive a password reset link.";
            }

            return View("Index");
        }

        [HttpGet]
        public IActionResult Reset(string token, string userId, string userType)
        {
            ViewBag.Token = token;
            ViewBag.UserId = userId;
            ViewBag.UserType = userType;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Reset(string token, string userId, string userType, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match";
                ViewBag.Token = token;
                ViewBag.UserId = userId;
                ViewBag.UserType = userType;
                return View();
            }

            var resetRequest = await _context.PasswordResets
                .FirstOrDefaultAsync(r => r.Token == token && r.UserId == userId && !r.IsUsed && r.ExpiresAt > DateTime.Now);

            if (resetRequest == null)
            {
                ViewBag.Error = "Invalid or expired reset link";
                return View();
            }

            if (userType == "Admin")
            {
                var admin = await _context.AdminUsers.FindAsync(int.Parse(userId));
                if (admin != null)
                {
                    admin.Password = password;
                }
            }
            else if (userType == "Provider")
            {
                var provider = await _context.Providers.FindAsync(int.Parse(userId));
                if (provider != null)
                {
                    provider.Password = password;
                }
            }
            else if (userType == "Enrollee")
            {
                var enrolleeUser = await _context.EnrolleeUsers.FindAsync(int.Parse(userId));
                if (enrolleeUser != null)
                {
                    enrolleeUser.PasswordHash = SonghaiHMO.Services.PasswordHasher.HashPassword(password);
                }
            }

            resetRequest.IsUsed = true;
            await _context.SaveChangesAsync();

            ViewBag.Success = "Password has been reset successfully. Please login with your new password.";
            return View("ResetSuccess");
        }

        public IActionResult ResetSuccess()
        {
            return View();
        }
    }
}