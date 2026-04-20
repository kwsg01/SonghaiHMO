using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Models;

namespace SonghaiHMO.Services
{
    public class ClaimService : IClaimService
    {
        private readonly ApplicationDbContext _context;

        public ClaimService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Claim>> GetAllClaimsAsync()
        {
            return await _context.Claims
                .Include(c => c.Enrollee)
                .Include(c => c.Provider)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();
        }

        public async Task<Claim?> GetClaimByIdAsync(int id)
        {
            return await _context.Claims
                .Include(c => c.Enrollee)
                .Include(c => c.Provider)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Claim> CreateClaimAsync(Claim claim)
        {
            claim.Status = "Pending";
            claim.SubmittedAt = DateTime.Now;
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
            return claim;
        }

        public async Task<Claim?> UpdateClaimStatusAsync(int id, string status, string notes)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = status;
                claim.AdminNotes = notes;
                await _context.SaveChangesAsync();
            }
            return claim;
        }

        public async Task<IEnumerable<Claim>> GetPendingClaimsAsync()
        {
            return await _context.Claims
                .Include(c => c.Enrollee)
                .Include(c => c.Provider)
                .Where(c => c.Status == "Pending")
                .OrderBy(c => c.SubmittedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Claim>> GetProviderClaimsAsync(int providerId)
        {
            return await _context.Claims
                .Include(c => c.Enrollee)
                .Where(c => c.ProviderId == providerId)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var claims = await _context.Claims.ToListAsync();
            var enrollees = await _context.Enrollees.ToListAsync();

            return new DashboardStats
            {
                TotalEnrollees = enrollees.Count,
                TotalClaims = claims.Count,
                PendingClaims = claims.Count(c => c.Status == "Pending"),
                ApprovedClaims = claims.Count(c => c.Status == "Approved"),
                RejectedClaims = claims.Count(c => c.Status == "Rejected"),
                TotalPayout = claims.Where(c => c.Status == "Approved").Sum(c => c.Amount)
            };
        }
    }
}