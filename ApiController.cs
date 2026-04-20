using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;
        private readonly GoogleAuthenticatorService _googleAuthService;

        public ApiController(ApplicationDbContext context, JwtService jwtService, GoogleAuthenticatorService googleAuthService)
        {
            _context = context;
            _jwtService = jwtService;
            _googleAuthService = googleAuthService;
        }

        // POST: api/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var admin = await _context.AdminUsers.FirstOrDefaultAsync(a => a.Username == request.Username && a.Password == request.Password);
            if (admin == null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid credentials", StatusCode = 401 });
            }

            var token = _jwtService.GenerateToken(admin.Id.ToString(), admin.Username, "Admin");
            var userData = new 
            { 
                Token = token, 
                User = new { admin.Id, admin.Username, admin.FullName, Role = "Admin" } 
            };
            
            return Ok(new ApiResponse<object> { Success = true, Message = "Login successful", Data = userData });
        }

        // GET: api/claims
        [HttpGet("claims")]
        public async Task<IActionResult> GetClaims([FromHeader] string Authorization)
        {
            var token = Authorization?.Replace("Bearer ", "");
            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid token" });
            }

            var claims = await _context.Claims
                .Include(c => c.Enrollee)
                .Include(c => c.Provider)
                .OrderByDescending(c => c.SubmittedAt)
                .Select(c => new
                {
                    c.Id,
                    c.EnrolleeId,
                    EnrolleeName = c.Enrollee != null ? c.Enrollee.FullName : "",
                    c.ProviderId,
                    c.ProviderName,
                    c.ServiceDescription,
                    c.Amount,
                    c.Status,
                    c.AdminNotes,
                    SubmittedAt = c.SubmittedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            return Ok(new ApiResponse<object> { Success = true, Data = claims });
        }

        // GET: api/claims/{id}
        [HttpGet("claims/{id}")]
        public async Task<IActionResult> GetClaim(int id, [FromHeader] string Authorization)
        {
            var token = Authorization?.Replace("Bearer ", "");
            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid token" });
            }

            var claim = await _context.Claims
                .Include(c => c.Enrollee)
                .Include(c => c.Provider)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "Claim not found" });
            }

            return Ok(new ApiResponse<object> { Success = true, Data = claim });
        }

        // POST: api/claims
        [HttpPost("claims")]
        public async Task<IActionResult> CreateClaim([FromBody] CreateClaimRequest request, [FromHeader] string Authorization)
        {
            var token = Authorization?.Replace("Bearer ", "");
            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid token" });
            }

            var newClaim = new SonghaiHMO.Models.Claim
            {
                EnrolleeId = request.EnrolleeId,
                ProviderId = request.ProviderId,
                ProviderName = request.ProviderName,
                ServiceDescription = request.ServiceDescription,
                Amount = request.Amount,
                Status = "Pending",
                SubmittedAt = DateTime.Now
            };

            _context.Claims.Add(newClaim);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object> { Success = true, Message = "Claim submitted successfully", Data = new { newClaim.Id } });
        }

        // GET: api/enrollees
        [HttpGet("enrollees")]
        public async Task<IActionResult> GetEnrollees([FromHeader] string Authorization)
        {
            var token = Authorization?.Replace("Bearer ", "");
            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid token" });
            }

            var enrollees = await _context.Enrollees
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new { e.Id, e.FirstName, e.LastName, FullName = e.FullName, e.Email, e.Phone, e.PlanType, e.CreatedAt })
                .ToListAsync();

            return Ok(new ApiResponse<object> { Success = true, Data = enrollees });
        }

        // GET: api/providers
        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders([FromHeader] string Authorization)
        {
            var token = Authorization?.Replace("Bearer ", "");
            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid token" });
            }

            var providers = await _context.Providers
                .OrderBy(p => p.ProviderName)
                .Select(p => new { p.Id, p.ProviderName, p.Email, p.Phone, p.Address })
                .ToListAsync();

            return Ok(new ApiResponse<object> { Success = true, Data = providers });
        }

        // GET: api/dashboard/stats
        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats([FromHeader] string Authorization)
        {
            var token = Authorization?.Replace("Bearer ", "");
            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid token" });
            }

            var totalEnrollees = await _context.Enrollees.CountAsync();
            var totalClaims = await _context.Claims.CountAsync();
            var pendingClaims = await _context.Claims.CountAsync(c => c.Status == "Pending");
            var approvedClaims = await _context.Claims.CountAsync(c => c.Status == "Approved");
            var rejectedClaims = await _context.Claims.CountAsync(c => c.Status == "Rejected");
            var totalPayout = await _context.Claims.Where(c => c.Status == "Approved").SumAsync(c => c.Amount);

            var statsData = new
            {
                TotalEnrollees = totalEnrollees,
                TotalClaims = totalClaims,
                PendingClaims = pendingClaims,
                ApprovedClaims = approvedClaims,
                RejectedClaims = rejectedClaims,
                TotalPayout = totalPayout
            };

            return Ok(new ApiResponse<object> { Success = true, Data = statsData });
        }

        // POST: api/enable-2fa
        [HttpPost("enable-2fa")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorSetupRequest request, [FromHeader] string Authorization)
        {
            var token = Authorization?.Replace("Bearer ", "");
            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid token" });
            }

            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "User ID not found" });
            }

            var admin = await _context.AdminUsers.FindAsync(int.Parse(userId));
            if (admin == null)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "User not found" });
            }

            var secretKey = _googleAuthService.GenerateSecretKey();
            var qrCodeUri = _googleAuthService.GenerateQrCodeUri(admin.Email, secretKey);

            var twoFactorData = new { SecretKey = secretKey, QrCodeUri = qrCodeUri };
            return Ok(new ApiResponse<object> { Success = true, Data = twoFactorData });
        }
    }

    // Request/Response Models
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateClaimRequest
    {
        public int EnrolleeId { get; set; }
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class TwoFactorSetupRequest
    {
        public bool Enable { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int StatusCode { get; set; } = 200;
    }
}