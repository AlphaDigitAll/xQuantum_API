using System.Security.Claims;

namespace xQuantum_API.Infrastructure
{
    /// <summary>
    /// Middleware that extracts and validates tenant context from authenticated requests.
    /// Runs after authentication middleware to ensure JWT has been validated.
    /// Stores OrgId and UserId in HttpContext.Items for easy access throughout the request pipeline.
    /// </summary>
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;

        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip tenant resolution for unauthenticated requests
            // (e.g., login, registration, health checks)
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            // Extract OrgId from JWT claims
            var orgIdClaim = context.User.FindFirst("OrgId")?.Value;
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = context.User.Identity?.Name;

            // Validate that authenticated user has required tenant information
            if (string.IsNullOrWhiteSpace(orgIdClaim))
            {
                _logger.LogWarning(
                    "Authenticated user missing OrgId claim. UserId: {UserId}, Username: {Username}, Path: {Path}",
                    userIdClaim ?? "unknown",
                    username ?? "unknown",
                    context.Request.Path);

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Invalid authentication token. Missing tenant information."
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                _logger.LogWarning(
                    "Authenticated user missing UserId claim. OrgId: {OrgId}, Username: {Username}, Path: {Path}",
                    orgIdClaim,
                    username ?? "unknown",
                    context.Request.Path);

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Invalid authentication token. Missing user information."
                });
                return;
            }

            // Store tenant context in HttpContext.Items for easy access
            context.Items["OrgId"] = orgIdClaim;
            context.Items["UserId"] = userIdClaim;
            context.Items["Username"] = username;

            _logger.LogDebug(
                "Tenant context resolved - OrgId: {OrgId}, UserId: {UserId}, Username: {Username}, Path: {Path}",
                orgIdClaim,
                userIdClaim,
                username,
                context.Request.Path);

            // Continue to next middleware
            await _next(context);
        }
    }

    /// <summary>
    /// Extension method for registering TenantResolutionMiddleware in the pipeline
    /// </summary>
    public static class TenantResolutionMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantResolutionMiddleware>();
        }
    }
}
