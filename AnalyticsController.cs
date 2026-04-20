using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonghaiHMO.Data;
using SonghaiHMO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonghaiHMO.Web.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId")))
                return RedirectToAction("Login", "Home", new { role = "admin" });

            var dashboard = await GetAnalyticsData();
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            return View(dashboard);
        }

        private async Task<AnalyticsDashboard> GetAnalyticsData()
        {
            var claims = await _context.Claims.ToListAsync();
            var enrollees = await _context.Enrollees.ToListAsync();
            var providers = await _context.Providers.ToListAsync();

            var totalClaims = claims.Count;
            var approvedClaims = claims.Count(c => c.Status == "Approved");
            var rejectedClaims = claims.Count(c => c.Status == "Rejected");
            var pendingClaims = claims.Count(c => c.Status == "Pending");
            var totalPayout = claims.Where(c => c.Status == "Approved").Sum(c => c.Amount);
            var averageClaimAmount = totalClaims > 0 ? totalPayout / totalClaims : 0;
            var approvalRate = totalClaims > 0 ? (double)approvedClaims / totalClaims * 100 : 0;
            var averageProcessingDays = claims.Where(c => c.Status == "Approved" && c.AdminNotes != null)
                .Average(c => (DateTime.Now - c.SubmittedAt).TotalDays);

            var thisMonth = DateTime.Now;
            var thisMonthClaims = claims.Count(c => c.SubmittedAt.Month == thisMonth.Month && c.SubmittedAt.Year == thisMonth.Year);
            var thisMonthPayout = claims.Where(c => c.Status == "Approved" && c.SubmittedAt.Month == thisMonth.Month && c.SubmittedAt.Year == thisMonth.Year)
                .Sum(c => c.Amount);

            var monthlyData = claims
                .GroupBy(c => new { c.SubmittedAt.Year, c.SubmittedAt.Month })
                .Select(g => new MonthlyData
                {
                    Year = g.Key.Year,
                    MonthNumber = g.Key.Month,
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Claims = g.Count(),
                    Amount = g.Sum(c => c.Amount)
                })
                .OrderByDescending(m => m.Year).ThenByDescending(m => m.MonthNumber)
                .Take(12)
                .ToList();

            var topProviders = await _context.Claims
                .GroupBy(c => new { c.ProviderId, c.ProviderName })
                .Select(g => new ProviderPerformance
                {
                    ProviderId = g.Key.ProviderId,
                    ProviderName = g.Key.ProviderName ?? "Unknown",
                    TotalClaims = g.Count(),
                    ApprovedClaims = g.Count(c => c.Status == "Approved"),
                    RejectedClaims = g.Count(c => c.Status == "Rejected"),
                    TotalAmount = g.Sum(c => c.Amount),
                    ApprovalRate = g.Count() > 0 ? (double)g.Count(c => c.Status == "Approved") / g.Count() * 100 : 0
                })
                .OrderByDescending(p => p.TotalClaims)
                .Take(10)
                .ToListAsync();

            var statusDistribution = new List<ClaimStatusData>
            {
                new ClaimStatusData { Status = "Pending", Count = pendingClaims, Amount = claims.Where(c => c.Status == "Pending").Sum(c => c.Amount), Percentage = totalClaims > 0 ? (double)pendingClaims / totalClaims * 100 : 0 },
                new ClaimStatusData { Status = "Approved", Count = approvedClaims, Amount = approvedClaims > 0 ? claims.Where(c => c.Status == "Approved").Sum(c => c.Amount) : 0, Percentage = totalClaims > 0 ? (double)approvedClaims / totalClaims * 100 : 0 },
                new ClaimStatusData { Status = "Rejected", Count = rejectedClaims, Amount = rejectedClaims > 0 ? claims.Where(c => c.Status == "Rejected").Sum(c => c.Amount) : 0, Percentage = totalClaims > 0 ? (double)rejectedClaims / totalClaims * 100 : 0 }
            };

            var dailyTrends = claims
                .GroupBy(c => c.SubmittedAt.Date)
                .Select(g => new DailyTrend
                {
                    Date = g.Key,
                    Claims = g.Count(),
                    Amount = g.Sum(c => c.Amount),
                    Approved = g.Count(c => c.Status == "Approved"),
                    Rejected = g.Count(c => c.Status == "Rejected")
                })
                .OrderByDescending(d => d.Date)
                .Take(30)
                .ToList();

            var recentEnrollees = enrollees
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .Select(e => new EnrolleeActivity
                {
                    EnrolleeId = e.Id,
                    Name = e.FullName,
                    Email = e.Email,
                    PlanType = e.PlanType,
                    ClaimCount = claims.Count(c => c.EnrolleeId == e.Id),
                    TotalAmount = claims.Where(c => c.EnrolleeId == e.Id && c.Status == "Approved").Sum(c => c.Amount),
                    JoinedAt = e.CreatedAt
                })
                .ToList();

            return new AnalyticsDashboard
            {
                KPIs = new KPIData
                {
                    TotalEnrollees = enrollees.Count,
                    TotalClaims = totalClaims,
                    PendingClaims = pendingClaims,
                    ApprovedClaims = approvedClaims,
                    RejectedClaims = rejectedClaims,
                    TotalPayout = totalPayout,
                    AverageClaimAmount = averageClaimAmount,
                    ApprovalRate = approvalRate,
                    AverageProcessingDays = averageProcessingDays,
                    ActiveProviders = providers.Count,
                    ThisMonthClaims = thisMonthClaims,
                    ThisMonthPayout = thisMonthPayout
                },
                MonthlyClaims = monthlyData,
                TopProviders = topProviders,
                StatusDistribution = statusDistribution,
                DailyTrends = dailyTrends,
                RecentEnrollees = recentEnrollees
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetKPIs()
        {
            var dashboard = await GetAnalyticsData();
            return Json(dashboard.KPIs);
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyTrends()
        {
            var dashboard = await GetAnalyticsData();
            return Json(dashboard.MonthlyClaims);
        }

        [HttpGet]
        public async Task<IActionResult> GetTopProviders()
        {
            var dashboard = await GetAnalyticsData();
            return Json(dashboard.TopProviders);
        }
    }
}