namespace SonghaiHMO.Models
{
    public enum UserRole
    {
        Admin,
        SuperAdmin,
        Provider,
        CallCentre,
        Viewer
    }

    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum PlanType
    {
        Basic,
        Standard,
        Premium
    }
}