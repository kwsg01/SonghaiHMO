using System;
using System.Collections.Generic;

namespace SonghaiHMO.Models
{
    public class AnalyticsDashboard
    {
        public KPIData KPIs { get; set; } = new KPIData();
        public List<MonthlyData> MonthlyClaims { get; set; } = new List<MonthlyData>();
        public List<ProviderPerformance> TopProviders { get; set; } = new List<ProviderPerformance>();
        public List<ClaimStatusData> StatusDistribution { get; set; } = new List<ClaimStatusData>();
        public List<DailyTrend> DailyTrends { get; set; } = new List<DailyTrend>();
        public List<EnrolleeActivity> RecentEnrollees { get; set; } = new List<EnrolleeActivity>();
    }

    public class KPIData
    {
        public int TotalEnrollees { get; set; }
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalPayout { get; set; }
        public decimal AverageClaimAmount { get; set; }
        public double ApprovalRate { get; set; }
        public double AverageProcessingDays { get; set; }
        public int ActiveProviders { get; set; }
        public int ThisMonthClaims { get; set; }
        public decimal ThisMonthPayout { get; set; }
    }

    public class MonthlyData
    {
        public string Month { get; set; } = string.Empty;
        public int Claims { get; set; }
        public decimal Amount { get; set; }
        public int Year { get; set; }
        public int MonthNumber { get; set; }
    }

    public class ProviderPerformance
    {
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public double ApprovalRate { get; set; }
        public double AvgProcessingDays { get; set; }
    }

    public class ClaimStatusData
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
    }

    public class DailyTrend
    {
        public DateTime Date { get; set; }
        public int Claims { get; set; }
        public decimal Amount { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
    }

    public class EnrolleeActivity
    {
        public int EnrolleeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public int ClaimCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}