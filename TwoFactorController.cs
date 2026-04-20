using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Services;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class TwoFactorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TwoFactorService _twoFactorService;

        public TwoFactorController(ApplicationDbContext context, TwoFactorService twoFactorService)
        {
            _context = context;
            _twoFactorService = twoFactorService;
        }

        [HttpPost]
        public async Task<IActionResult> SendCode([FromBody] TwoFactorSendCodeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Email))
            {
                return Json(new { success = false, message = "Invalid request" });
            }
            await _twoFactorService.SendTwoFactorCode(request.UserId, request.Email);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyCode([FromBody] TwoFactorVerifyCodeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Code))
            {
                return Json(new { success = false, message = "Invalid request" });
            }
            var isValid = await _twoFactorService.VerifyCode(request.UserId, request.Code);
            
            if (isValid)
            {
                HttpContext.Session.SetString("TwoFactorVerified", request.UserId);
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Invalid or expired code" });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteLogin([FromBody] TwoFactorCompleteLoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.UserType))
            {
                return Json(new { success = false, message = "Invalid request" });
            }
            var verifiedUserId = HttpContext.Session.GetString("TwoFactorVerified");
            if (verifiedUserId != request.UserId)
                return Json(new { success = false, message = "Verification required" });

            HttpContext.Session.Remove("TwoFactorVerified");

            if (request.UserType == "Admin")
            {
                var admin = await _context.AdminUsers.FindAsync(int.Parse(request.UserId));
                if (admin != null)
                {
                    HttpContext.Session.SetString("AdminId", admin.Id.ToString());
                    HttpContext.Session.SetString("AdminName", admin.FullName);
                    HttpContext.Session.SetString("AdminRole", admin.Role);
                    return Json(new { success = true, redirectUrl = "/Admin/Dashboard" });
                }
            }
            else if (request.UserType == "Provider")
            {
                var provider = await _context.Providers.FindAsync(int.Parse(request.UserId));
                if (provider != null)
                {
                    HttpContext.Session.SetString("ProviderId", provider.Id.ToString());
                    HttpContext.Session.SetString("ProviderName", provider.ProviderName);
                    return Json(new { success = true, redirectUrl = "/Provider/Dashboard" });
                }
            }

            return Json(new { success = false, message = "User not found" });
        }
    }

    // Request classes
    public class TwoFactorSendCodeRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class TwoFactorVerifyCodeRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class TwoFactorCompleteLoginRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
    }
}