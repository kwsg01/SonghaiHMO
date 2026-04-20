using System.Collections.Generic;
using System.Threading.Tasks;
using SonghaiHMO.Models;

namespace SonghaiHMO.Services
{
    public interface IClaimService
    {
        Task<IEnumerable<Claim>> GetAllClaimsAsync();
        Task<Claim?> GetClaimByIdAsync(int id);
        Task<Claim> CreateClaimAsync(Claim claim);
        Task<Claim?> UpdateClaimStatusAsync(int id, string status, string notes);
        Task<IEnumerable<Claim>> GetPendingClaimsAsync();
        Task<IEnumerable<Claim>> GetProviderClaimsAsync(int providerId);
        Task<DashboardStats> GetDashboardStatsAsync();
    }
}