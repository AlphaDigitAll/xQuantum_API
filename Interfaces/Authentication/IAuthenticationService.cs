using System.Security.Claims;
using xQuantum_API.Models.Tenant;

namespace xQuantum_API.Interfaces.Authentication
{
    public interface IAuthenticationService
    {
        string GenerateJwtToken(TenantInfo tenantInfo);
        ClaimsPrincipal ValidateToken(string token);
        string GetOrgIdFromToken(ClaimsPrincipal principal);
        string GetUserIdFromToken(ClaimsPrincipal principal);
    }
}
