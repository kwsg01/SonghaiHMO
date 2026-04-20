using SonghaiHMO.Data;
using SonghaiHMO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SonghaiHMO.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateClaimReportExcel(DateTime startDate, DateTime endDate, string status, int? providerId)
        {
            var query = _context.Claims
                .Include(c => c.Enrollee)
                .Include(c => c.Provider)
                .Where(c => c.SubmittedAt.Date >= startDate.Date && c.SubmittedAt.Date <= endDate.Date);

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(c => c.Status == status);

            if (providerId.HasValue && providerId.Value > 0)
                query = query.Where(c => c.ProviderId == providerId.Value);

            var claims = await query.OrderByDescending(c => c.SubmittedAt).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Claim ID,Enrollee,Provider,Service,Amount,Status,Date,Notes");

            foreach (var claim in claims)
            {
                csv.AppendLine($"\"{claim.Id}\",\"{claim.Enrollee?.FullName}\",\"{claim.ProviderName}\",\"{claim.ServiceDescription}\",{claim.Amount},\"{claim.Status}\",\"{claim.SubmittedAt:yyyy-MM-dd}\",\"{claim.AdminNotes}\"");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}