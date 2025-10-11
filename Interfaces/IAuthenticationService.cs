using System.Security.Claims;
using xQuantum_API.Models;

namespace xQuantum_API.Interfaces
{
    public interface IAuthenticationService
    {
        string GenerateJwtToken(TenantInfo tenantInfo);
        ClaimsPrincipal ValidateToken(string token);
        string GetOrgIdFromToken(ClaimsPrincipal principal);
        string GetUserIdFromToken(ClaimsPrincipal principal);
    }
}
