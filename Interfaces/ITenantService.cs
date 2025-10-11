using xQuantum_API.Models;

namespace xQuantum_API.Interfaces
{
    public interface ITenantService
    {
        Task<TenantInfo> ValidateAndGetTenantInfoAsync(string username, string password);
        Task<string> GetTenantConnectionStringAsync(string orgId);

    }
}
