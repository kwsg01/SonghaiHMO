using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Models;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class EnrolleeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EnrolleeController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var enrolleeUser = await _context.EnrolleeUsers
                .Include(e => e.Enrollee)
                .FirstOrDefaultAsync(e => e.Username == username);

            if (enrolleeUser != null && enrolleeUser.PasswordHash == HashPassword(password))
            {
                enrolleeUser.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("EnrolleeId", enrolleeUser.EnrolleeId.ToString());
                HttpContext.Session.SetString("EnrolleeName", enrolleeUser.Enrollee.FullName);
                HttpContext.Session.SetString("EnrolleeUsername", enrolleeUser.Username);

                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid username or password";
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var enrolleeId = HttpContext.Session.GetString("EnrolleeId");
            if (string.IsNullOrEmpty(enrolleeId))
                return RedirectToAction("Login");

            var enrollee = await _context.Enrollees.FindAsync(int.Parse(enrolleeId));
            var claims = await _context.Claims
                .Where(c => c.EnrolleeId == int.Parse(enrolleeId))
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();

            ViewBag.Enrollee = enrollee;
            ViewBag.Claims = claims;
            ViewBag.TotalClaims = claims.Count;
            ViewBag.ApprovedAmount = claims.Where(c => c.Status == "Approved").Sum(c => c.Amount);
            ViewBag.PendingClaims = claims.Count(c => c.Status == "Pending");

            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}