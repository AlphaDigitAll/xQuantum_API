using Microsoft.AspNetCore.Mvc;

namespace xQuantum_API.Controllers
{
    /// <summary>
    /// Base controller for all tenant-aware endpoints.
    /// Provides easy access to tenant context (OrgId, UserId) that is set by TenantResolutionMiddleware.
    /// All controllers that require tenant isolation should inherit from this class.
    /// </summary>
    public abstract class TenantAwareControllerBase : ControllerBase
    {
        /// <summary>
        /// Gets the organization ID (tenant ID) for the current authenticated request.
        /// This value is extracted from JWT claims by TenantResolutionMiddleware.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if OrgId is not found in HttpContext.Items.
        /// This indicates TenantResolutionMiddleware is not properly registered or user is not authenticated.
        /// </exception>
        protected string OrgId
        {
            get
            {
                var orgId = HttpContext.Items["OrgId"]?.ToString();

                if (string.IsNullOrWhiteSpace(orgId))
                {
                    throw new InvalidOperationException(
                        "OrgId not found in request context. " +
                        "Ensure TenantResolutionMiddleware is registered after UseAuthentication() in Program.cs");
                }

                return orgId;
            }
        }

        /// <summary>
        /// Gets the user ID for the current authenticated request.
        /// This value is extracted from JWT claims by TenantResolutionMiddleware.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if UserId is not found in HttpContext.Items.
        /// </exception>
        protected string UserId
        {
            get
            {
                var userId = HttpContext.Items["UserId"]?.ToString();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new InvalidOperationException(
                        "UserId not found in request context. " +
                        "Ensure TenantResolutionMiddleware is registered after UseAuthentication() in Program.cs");
                }

                return userId;
            }
        }

        /// <summary>
        /// Gets the user ID as a Guid for the current authenticated request.
        /// Convenience property for methods that require Guid format.
        /// </summary>
        protected Guid UserIdGuid
        {
            get
            {
                if (Guid.TryParse(UserId, out var userIdGuid))
                {
                    return userIdGuid;
                }

                throw new InvalidOperationException($"UserId '{UserId}' is not a valid GUID");
            }
        }

        /// <summary>
        /// Gets the username for the current authenticated request.
        /// </summary>
        protected string Username => HttpContext.Items["Username"]?.ToString() ?? "unknown";
    }
}
