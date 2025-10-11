using Npgsql;
using xQuantum_API.Interfaces;
using xQuantum_API.Models;

namespace xQuantum_API.Services
{
    public class TenantService: ITenantService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TenantService> _logger;
        private readonly string _masterConnectionString;

        public TenantService(IConfiguration configuration,ILogger<TenantService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _masterConnectionString = _configuration.GetConnectionString("MasterDatabase");
        }

        /// <summary>
        /// Validate user credentials and get tenant information
        /// </summary>
        public async Task<TenantInfo> ValidateAndGetTenantInfoAsync(string userEmail, string password)
        {
            try
            {
                _logger.LogInformation("Validating user: {UserEmail}", userEmail);

                await using var connection = new NpgsqlConnection(_masterConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                    u.user_id,
                    u.first_name || ' ' || u.last_name as username,
                    u.password,
                    u.org_id,
                    o.org_name,
                    o.is_vendor_lwa,
                    o.is_seller_lwa,
                    o.is_ads_lwa
                FROM user_master u
                INNER JOIN org_master o ON u.org_id = o.org_id
                WHERE u.email = @useremail 
                  AND u.is_active = true and u.is_verified =true
                  AND o.is_active = true and o.is_verified = true";

                await using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@useremail", userEmail);

                await using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    _logger.LogWarning("User not found or inactive: {UserEmail}", userEmail);
                    return null;
                }
                var passwordHash = reader.GetString(reader.GetOrdinal("password"));

                // Verify password (use BCrypt or similar in production)
                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                {
                    _logger.LogWarning("Invalid password for user: {UserEmail}", userEmail);
                    return null;
                }
                //var dbPassword = reader.GetString(reader.GetOrdinal("password"));

                // 🔸 For now, direct comparison(later replace with BCrypt)
                //if (!string.Equals(password, dbPassword, StringComparison.Ordinal))
                //{
                //    _logger.LogWarning("Invalid password for user: {UserEmail}", userEmail);
                //    return null;
                //}

                var tenantInfo = new TenantInfo
                {
                    UserId = reader.GetGuid(reader.GetOrdinal("user_id")).ToString(),
                    Username = reader.GetString(reader.GetOrdinal("username")),
                    OrgId = reader.GetGuid(reader.GetOrdinal("org_id")).ToString(),
                    OrgName = reader.GetString(reader.GetOrdinal("org_name")),
                    IsSellerLWA = reader.GetBoolean(reader.GetOrdinal("is_seller_lwa")),
                    IsAdsLWA = reader.GetBoolean(reader.GetOrdinal("is_ads_lwa")),
                    IsVendorLWA = reader.GetBoolean(reader.GetOrdinal("is_vendor_lwa"))

                };

                _logger.LogInformation("User validated successfully: {UserEmail}, Tenant: {OrgId}",
                    userEmail, tenantInfo.OrgId);

                return tenantInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user: {UserEmail}", userEmail);
                throw;
            }
        }

        /// <summary>
        /// Get tenant-specific connection string from master database
        /// </summary>
        public async Task<string> GetTenantConnectionStringAsync(string orgId)
        {
            try
            {
                _logger.LogInformation("Fetching connection string for OrgId: {OrgId}", orgId);

                await using var connection = new NpgsqlConnection(_masterConnectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT 
                c.host,
                c.port,
                c.db_name,
                c.username AS db_username,
                c.password AS db_password
            FROM org_master o
            INNER JOIN con_master c ON o.con_id = c.con_id
            WHERE o.org_id = @orgId
              AND o.is_active = true
              AND c.is_active = true";

                await using var command = new NpgsqlCommand(query, connection);
                if (!Guid.TryParse(orgId, out Guid orgGuid))
                    throw new ArgumentException($"Invalid OrgId format: {orgId}");

                command.Parameters.AddWithValue("@orgId", NpgsqlTypes.NpgsqlDbType.Uuid, orgGuid);
                await using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException($"Organization not found or inactive: {orgId}");
                }

                var dbHost = reader.GetString(reader.GetOrdinal("host"));
                var dbPort = reader.GetInt32(reader.GetOrdinal("port"));
                var dbName = reader.GetString(reader.GetOrdinal("db_name"));
                var dbUser = reader.GetString(reader.GetOrdinal("db_username"));
                var dbPassword = reader.GetString(reader.GetOrdinal("db_password"));

                // ✅ Build PostgreSQL connection string
                var connectionString =
                    $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};" +
                    "Pooling=true;MinPoolSize=5;MaxPoolSize=100;Connection Idle Lifetime=300;Connection Pruning Interval=10;";

                _logger.LogInformation("✅ Connection string built successfully for OrgId: {OrgId}", orgId);
                return connectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching connection string for OrgId: {OrgId}", orgId);
                throw;
            }
        }

    }
}
