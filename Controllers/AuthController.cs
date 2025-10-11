using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Infrastructure;
using xQuantum_API.Interfaces;
using xQuantum_API.Models;
using xQuantum_API.Models.Login;

namespace xQuantum_API.Controllers
{

    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthController : ControllerBase
    {
        private readonly ITenantService _tenantService;
        private readonly IAuthenticationService _authService;
        private readonly IConnectionStringManager _connectionManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ITenantService tenantService,
            IAuthenticationService authService,
            IConnectionStringManager connectionManager,
            ILogger<AuthController> logger)
        {
            _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// User login endpoint
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {UserEmail}", request.UserEmail);

                if (string.IsNullOrWhiteSpace(request.UserEmail) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "UserEmail and password are required" });
                }

                // Validate user and get tenant info
                var tenantInfo = await _tenantService.ValidateAndGetTenantInfoAsync(request.UserEmail, request.Password);

                if (tenantInfo == null)
                {
                    _logger.LogWarning("Login failed for user: {UserEmail}", request.UserEmail);
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // Pre-cache the connection string for better performance
                await _connectionManager.GetOrAddConnectionStringAsync(tenantInfo.OrgId.ToString(),
                    async () => await _tenantService.GetTenantConnectionStringAsync(tenantInfo.OrgId.ToString())
                );

                // Generate JWT token
                var token = _authService.GenerateJwtToken(tenantInfo);

                _logger.LogInformation("Login successful for user: {UserEmail}, Tenant: {OrgId}",
                    request.UserEmail, tenantInfo.OrgId.ToString());

                return Ok(new LoginResponse
                {
                    Token = token,
                    Username = tenantInfo.Username,
                    IsVendorLWA = tenantInfo.IsVendorLWA,
                    IsSellerLWA = tenantInfo.IsSellerLWA,
                    IsAdsLWA = tenantInfo.IsAdsLWA,
                    OrgId = tenantInfo.OrgId.ToString(),
                    OrgName = tenantInfo.OrgName,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Should match JWT expiration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {UserEmail}", request.UserEmail);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Token refresh endpoint
        /// </summary>
        [HttpPost("refresh")]
        [Authorize]
        public IActionResult RefreshToken()
        {
            try
            {
                var orgId = _authService.GetOrgIdFromToken(User);
                var userId = _authService.GetUserIdFromToken(User);
                var username = User.Identity?.Name;
                var isAdslwa = User?.FindFirst("IsAdsLWA")?.Value;
                var isSellerlwa = User?.FindFirst("IsSellerLWA")?.Value;
                var isVendorlwa = User?.FindFirst("IsVendorLWA")?.Value;

                if (string.IsNullOrWhiteSpace(orgId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Create new tenant info from existing claims
                var tenantInfo = new TenantInfo
                {
                    UserId = userId,
                    Username = username,
                    OrgId = orgId,
                    OrgName = User.FindFirst("OrgName")?.Value,
                    IsVendorLWA = Convert.ToBoolean(isVendorlwa),
                    IsSellerLWA = Convert.ToBoolean(isSellerlwa),
                    IsAdsLWA = Convert.ToBoolean(isAdslwa),
                };

                var newToken = _authService.GenerateJwtToken(tenantInfo);

                _logger.LogInformation("Token refreshed for user: {Username}, Tenant: {OrgId}",
                    username, orgId);

                return Ok(new LoginResponse
                {
                    Token = newToken,
                    Username = username,
                    OrgId = orgId,
                    IsVendorLWA = tenantInfo.IsVendorLWA,
                    IsSellerLWA = tenantInfo.IsSellerLWA,
                    IsAdsLWA = tenantInfo.IsAdsLWA,
                    OrgName = tenantInfo.OrgName,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "An error occurred during token refresh" });
            }
        }

        /// <summary>
        /// Logout endpoint (clears cached connection)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            try
            {
                var orgId = _authService.GetOrgIdFromToken(User);

                if (!string.IsNullOrWhiteSpace(orgId))
                {
                    // Optional: Remove connection string from cache on logout
                    // _connectionManager.RemoveConnectionString(orgId);

                    _logger.LogInformation("User logged out, Tenant: {OrgId}", orgId);
                }

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }
    }





}
