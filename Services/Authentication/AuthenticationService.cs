using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using xQuantum_API.Interfaces.Authentication;
using xQuantum_API.Models.Tenant;

namespace xQuantum_API.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationMinutes;

        public AuthenticationService( IConfiguration configuration, ILogger<AuthenticationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jwtSecret = _configuration["Jwt:Secret"];
            _jwtIssuer = _configuration["Jwt:Issuer"];
            _jwtAudience = _configuration["Jwt:Audience"];
            _jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        }

        /// <summary>
        /// Generate JWT token with tenant information
        /// </summary>
        public string GenerateJwtToken(TenantInfo tenantInfo)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, tenantInfo.UserId.ToString()),
                    new Claim(ClaimTypes.Name, tenantInfo.Username),
                    new Claim("OrgId", tenantInfo.OrgId.ToString()),
                    new Claim("OrgName", tenantInfo.OrgName),
                    new Claim("IsAdsLWA", tenantInfo.IsAdsLWA.ToString()),
                    new Claim("IsSellerLWA", tenantInfo.IsSellerLWA.ToString()),
                    new Claim("IsVendorLWA", tenantInfo.IsVendorLWA.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                    Issuer = _jwtIssuer,
                    Audience = _jwtAudience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation("JWT token generated for user: {Username}, Tenant: {OrgId}",
                    tenantInfo.Username, tenantInfo.OrgId);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user: {Username}", tenantInfo.Username);
                throw;
            }
        }

        /// <summary>
        /// Validate JWT token and return claims principal
        /// </summary>
        public ClaimsPrincipal ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }

        /// <summary>
        /// Extract tenant ID from claims
        /// </summary>
        public string GetOrgIdFromToken(ClaimsPrincipal principal)
        {
            return principal?.FindFirst("OrgId")?.Value;
        }

        /// <summary>
        /// Extract user ID from claims
        /// </summary>
        public string GetUserIdFromToken(ClaimsPrincipal principal)
        {
            return  principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

}
