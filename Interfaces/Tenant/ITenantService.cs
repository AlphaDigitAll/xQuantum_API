using xQuantum_API.Models.Tenant;

namespace xQuantum_API.Interfaces.Tenant
{
    public interface ITenantService
    {
        Task<TenantInfo> ValidateAndGetTenantInfoAsync(string username, string password);
        Task<string> GetTenantConnectionStringAsync(string orgId);

    }
}
