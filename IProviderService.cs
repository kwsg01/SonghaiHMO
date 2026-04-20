using System.Collections.Generic;
using System.Threading.Tasks;
using SonghaiHMO.Models;

namespace SonghaiHMO.Services
{
    public interface IProviderService
    {
        Task<IEnumerable<Provider>> GetAllProvidersAsync();
        Task<Provider?> GetProviderByIdAsync(int id);
        Task<Provider?> GetProviderByEmailAsync(string email);
        Task<Provider> CreateProviderAsync(Provider provider);
        Task<Provider?> UpdateProviderAsync(int id, Provider provider);
        Task<bool> DeleteProviderAsync(int id);
        Task<bool> ValidateProviderLoginAsync(string email, string password);
    }
}