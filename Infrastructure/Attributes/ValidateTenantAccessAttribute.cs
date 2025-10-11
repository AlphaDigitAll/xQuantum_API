using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace xQuantum_API.Infrastructure.Attributes
{
    /// <summary>
    /// Action filter attribute that validates tenant access for controller actions.
    /// Ensures that the OrgId from JWT token matches any orgId parameters in the request.
    /// Prevents unauthorized cross-tenant data access attempts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ValidateTenantAccessAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Get OrgId from HttpContext (set by TenantResolutionMiddleware)
            var contextOrgId = context.HttpContext.Items["OrgId"]?.ToString();

            if (string.IsNullOrWhiteSpace(contextOrgId))
            {
                var logger = context.HttpContext.RequestServices
                    .GetService<ILogger<ValidateTenantAccessAttribute>>();

                logger?.LogWarning(
                    "ValidateTenantAccess: Tenant context not found. Path: {Path}, User: {User}",
                    context.HttpContext.Request.Path,
                    context.HttpContext.User.Identity?.Name ?? "unknown");

                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Tenant context not found. Please ensure you are properly authenticated."
                });
                return;
            }

            // Check if action has orgId parameter from route/query/body
            var orgIdParam = context.ActionArguments
                .FirstOrDefault(x => x.Key.Equals("orgId", StringComparison.OrdinalIgnoreCase));

            // If orgId parameter exists, validate it matches the JWT's OrgId
            if (orgIdParam.Key != null)
            {
                var requestedOrgId = orgIdParam.Value?.ToString();

                if (!string.IsNullOrWhiteSpace(requestedOrgId) && requestedOrgId != contextOrgId)
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<ValidateTenantAccessAttribute>>();

                    logger.LogWarning(
                        "Tenant access violation attempt detected. " +
                        "JWT OrgId: {JwtOrgId}, Requested OrgId: {RequestedOrgId}, " +
                        "UserId: {UserId}, Path: {Path}, IP: {IpAddress}",
                        contextOrgId,
                        requestedOrgId,
                        context.HttpContext.Items["UserId"],
                        context.HttpContext.Request.Path,
                        context.HttpContext.Connection.RemoteIpAddress);

                    context.Result = new UnauthorizedObjectResult(new
                    {
                        success = false,
                        message = "Access denied. You are not authorized to access data for the requested organization."
                    });
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
