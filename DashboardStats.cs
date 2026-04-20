namespace SonghaiHMO.Models
{
    public class DashboardStats
    {
        public int TotalEnrollees { get; set; }
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalPayout { get; set; }
    }
}