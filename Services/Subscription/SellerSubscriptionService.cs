using Newtonsoft.Json;
using Npgsql;
using RestSharp;
using xQuantum_API.Interfaces.Subscription;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.Subscription;

namespace xQuantum_API.Services.Subscription
{
    /// <summary>
    /// Service for managing Amazon Seller subscriptions (seller account connections).
    ///
    /// IMPORTANT DATABASE ARCHITECTURE:
    /// ================================
    /// seller_subscriptions table is stored in the MASTER DATABASE, NOT in tenant databases!
    /// - All subscription data is centralized in one database
    /// - orgId is used to filter data per organization
    /// - This is different from other services that use tenant-specific databases
    ///
    /// RestSharp Library Usage:
    /// -----------------------
    /// RestSharp is a popular HTTP client library for .NET that simplifies making REST API calls.
    ///
    /// Why we use RestSharp here:
    /// 1. Simplified HTTP Requests: Provides a clean, fluent API for making HTTP requests
    /// 2. Amazon SP-API Integration: Used to exchange OAuth authorization codes for access/refresh tokens
    /// 3. Automatic Serialization: Automatically serializes request bodies to JSON
    /// 4. Better Error Handling: Provides structured response objects with status codes
    /// 5. Less Boilerplate: Reduces code compared to using HttpClient directly
    ///
    /// Alternative: You could use HttpClient (built-in .NET), but RestSharp requires less code:
    /// - RestSharp: 5-6 lines for a POST request with JSON body
    /// - HttpClient: 10-12 lines for the same operation
    ///
    /// In this service, RestSharp is used in GetAccessTokenFromAmazonAsync() to:
    /// - POST to Amazon's token endpoint
    /// - Send OAuth code + client credentials
    /// - Receive access_token, refresh_token, and expires_in
    /// </summary>
    public class SellerSubscriptionService : ISellerSubscriptionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SellerSubscriptionService> _logger;
        private readonly string _masterConnectionString;

        public SellerSubscriptionService(
            IConfiguration configuration,
            ILogger<SellerSubscriptionService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _masterConnectionString = _configuration.GetConnectionString("MasterDatabase")
                ?? throw new InvalidOperationException("Master database connection string not configured");
        }

        public async Task<ApiResponse<Guid>> CreateSubscriptionAsync(string orgId, CreateSubscriptionRequest request, Guid createdBy)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_masterConnectionString);
                await conn.OpenAsync();

                var subId = Guid.NewGuid();

                var sql = @"
                    INSERT INTO seller_subscriptions
                        (sub_id, org_id, sub_name, region, country, marketplace_id, gmt,
                         is_active, old_data_load, loading_in_progress, last_updated)
                    VALUES
                        (@sub_id, @org_id, @sub_name, @region, @country, @marketplace_id, @gmt,
                         true, false, true, @last_updated)
                    RETURNING sub_id;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@sub_id", subId);
                cmd.Parameters.AddWithValue("@org_id", Guid.Parse(orgId));
                cmd.Parameters.AddWithValue("@sub_name", request.SubName);
                cmd.Parameters.AddWithValue("@region", request.Region);
                cmd.Parameters.AddWithValue("@country", request.Country);
                cmd.Parameters.AddWithValue("@marketplace_id", request.MarketplaceId);
                cmd.Parameters.AddWithValue("@gmt", request.Gmt);
                cmd.Parameters.AddWithValue("@last_updated", DateTime.UtcNow);

                var result = (Guid)await cmd.ExecuteScalarAsync();

                // ============================================================================
                // TODO: COMMENTED OUT - tbl_user_currency_formatting integration
                // ============================================================================
                // This was inserting currency formatting for subscriptions
                // Table: tbl_user_currency_formatting
                // Uncomment and implement when the table schema is confirmed
                // await InsertCurrencyFormattingAsync(conn, orgId, subId, request.Country);
                // ============================================================================

                _logger.LogInformation("Subscription created successfully. SubId: {SubId}, OrgId: {OrgId}", result, orgId);
                return ApiResponse<Guid>.Ok(result, "Subscription created successfully");
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23505")
            {
                _logger.LogWarning(pgEx, "Duplicate subscription for OrgId: {OrgId}", orgId);
                return ApiResponse<Guid>.Fail("A subscription with this information already exists.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription for OrgId: {OrgId}", orgId);
                return ApiResponse<Guid>.Fail($"Error creating subscription: {ex.Message}");
            }
        }

        // ============================================================================
        // COMMENTED OUT - Currency Formatting Methods (uses tbl_user_currency_formatting)
        // ============================================================================
        // Uncomment these methods when tbl_user_currency_formatting table is ready
        //
        // private async Task InsertCurrencyFormattingAsync(NpgsqlConnection conn, string orgId, Guid subId, string country)
        // {
        //     var symbol = GetCurrencySymbol(country);
        //
        //     var sql = @"
        //         INSERT INTO tbl_user_currency_formatting
        //             (org_id, sub_id, symbol, symbol_position, amount_position, number_of_decimal, formatter, currency_group)
        //         VALUES
        //             (@org_id, @sub_id, @symbol, 1, 'text-end', 0, 0, false);";
        //
        //     await using var cmd = new NpgsqlCommand(sql, conn);
        //     cmd.Parameters.AddWithValue("@org_id", Guid.Parse(orgId));
        //     cmd.Parameters.AddWithValue("@sub_id", subId);
        //     cmd.Parameters.AddWithValue("@symbol", symbol);
        //
        //     await cmd.ExecuteNonQueryAsync();
        // }
        //
        // private string GetCurrencySymbol(string country)
        // {
        //     return country switch
        //     {
        //         "Brazil" => "R$",
        //         "Spain" or "France" or "Belgium" or "Netherlands" or "Germany" or "Italy" => "€",
        //         "Sweden" => "kr",
        //         "South Africa" => "R",
        //         "Poland" => "zł",
        //         "Egypt" => "ج.م.",
        //         "Turkey" => "₺",
        //         "Saudi Arabia" => "ر.س.‏",
        //         "Singapore" or "USA" or "Canada" or "Mexico" or "Australia" => "$",
        //         "Japan" => "¥",
        //         "UAE" => "د.إ.‏",
        //         "UK" => "£",
        //         "India" => "₹",
        //         _ => "$"
        //     };
        // }
        // ============================================================================

        public async Task<ApiResponse<bool>> AuthenticateAndSaveSellerDataAsync(string orgId, AuthenticateSellerRequest request)
        {
            NpgsqlConnection? conn = null;
            NpgsqlTransaction? transaction = null;

            try
            {
                conn = new NpgsqlConnection(_masterConnectionString);
                await conn.OpenAsync();
                transaction = await conn.BeginTransactionAsync();

                // Check if subscription already exists
                var exists = await SubscriptionExistsAsync(orgId, request.SellingPartnerId, request.MwsAuthToken);
                if (exists)
                {
                    throw new InvalidOperationException("This seller account is already connected");
                }

                // Get access token from Amazon SP-API
                var tokenResponse = await GetAccessTokenFromAmazonAsync(request.SpapiOauthCode);
                if (tokenResponse == null)
                {
                    throw new InvalidOperationException("Failed to generate access token from Amazon");
                }

                // Update subscription with authentication details
                var sql = @"
                    UPDATE seller_subscriptions
                    SET
                        spapi_oauth_code = @spapi_oauth_code,
                        mws_auth_token = @mws_auth_token,
                        auth_on = @auth_on,
                        is_active = true,
                        refresh_token = @refresh_token,
                        expires_on = @expires_on,
                        selling_partner_id = @selling_partner_id,
                        old_data_load = false,
                        loading_in_progress = false,
                        last_updated = @last_updated
                    WHERE sub_id = @sub_id AND org_id = @org_id;";

                await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@spapi_oauth_code", request.SpapiOauthCode);
                cmd.Parameters.AddWithValue("@mws_auth_token", request.MwsAuthToken);
                cmd.Parameters.AddWithValue("@auth_on", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@refresh_token", tokenResponse.RefreshToken);
                cmd.Parameters.AddWithValue("@expires_on", DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
                cmd.Parameters.AddWithValue("@selling_partner_id", request.SellingPartnerId);
                cmd.Parameters.AddWithValue("@last_updated", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@sub_id", request.SubId);
                cmd.Parameters.AddWithValue("@org_id", Guid.Parse(orgId));

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException("Subscription not found or access denied");
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Seller authenticated successfully. SubId: {SubId}, OrgId: {OrgId}", request.SubId, orgId);
                return ApiResponse<bool>.Ok(true, "Seller authenticated successfully");
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Error rolling back transaction");
                    }
                }

                _logger.LogError(ex, "Error authenticating seller for SubId: {SubId}, OrgId: {OrgId}", request.SubId, orgId);
                return ApiResponse<bool>.Fail($"Error authenticating seller: {ex.Message}");
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync();
                if (conn != null)
                    await conn.DisposeAsync();
            }
        }

        /// <summary>
        /// ============================================================================
        /// RESTSHARP USAGE - Amazon SP-API OAuth Token Exchange
        /// ============================================================================
        /// Exchanges an OAuth authorization code for access/refresh tokens from Amazon SP-API.
        ///
        /// WHY RESTSHARP:
        /// --------------
        /// RestSharp simplifies HTTP REST API calls. This method demonstrates typical RestSharp usage:
        ///
        /// 1. Create RestClient (no base URL needed for single requests)
        /// 2. Create RestRequest with URL and HTTP method
        /// 3. Add JSON body using AddJsonBody() - automatically serializes objects
        /// 4. Execute request with ExecutePostAsync()
        /// 5. Check response.IsSuccessful and parse response.Content
        ///
        /// ALTERNATIVE WITH HTTPCLIENT (more verbose):
        /// -------------------------------------------
        /// using var client = new HttpClient();
        /// var content = new StringContent(
        ///     JsonConvert.SerializeObject(new { grant_type = "...", code = "...", ... }),
        ///     Encoding.UTF8,
        ///     "application/json"
        /// );
        /// var response = await client.PostAsync(tokenUrl, content);
        /// var responseBody = await response.Content.ReadAsStringAsync();
        ///
        /// CONFIGURATION REQUIRED (appsettings.json):
        /// ------------------------------------------
        /// "Amazon": {
        ///   "TokenUrl": "https://api.amazon.com/auth/o2/token",
        ///   "ClientId": "amzn1.application-oa2-client.xxxxx",
        ///   "ClientSecret": "your-client-secret-here"
        /// }
        ///
        /// OAUTH FLOW:
        /// -----------
        /// 1. User authorizes app on Amazon Seller Central
        /// 2. Amazon redirects with authorization code (spapi_oauth_code)
        /// 3. This method exchanges code for access_token + refresh_token
        /// 4. access_token: Short-lived (1 hour), used for API calls
        /// 5. refresh_token: Long-lived (365 days), used to get new access tokens
        ///
        /// RESPONSE STRUCTURE (from Amazon):
        /// ---------------------------------
        /// {
        ///   "access_token": "Atza|...",
        ///   "refresh_token": "Atzr|...",
        ///   "token_type": "bearer",
        ///   "expires_in": 3600
        /// }
        /// ============================================================================
        /// </summary>
        private async Task<AccessTokenResponse?> GetAccessTokenFromAmazonAsync(string oauthCode)
        {
            try
            {
                var tokenUrl = _configuration["Amazon:TokenUrl"] ?? throw new InvalidOperationException("Amazon TokenUrl not configured");
                var clientId = _configuration["Amazon:ClientId"] ?? throw new InvalidOperationException("Amazon ClientId not configured");
                var clientSecret = _configuration["Amazon:ClientSecret"] ?? throw new InvalidOperationException("Amazon ClientSecret not configured");

                // RestSharp: Create client (no base URL for single requests)
                var client = new RestClient();

                // RestSharp: Create POST request with URL
                var request = new RestRequest(tokenUrl, Method.Post);

                // RestSharp: Add JSON body - automatically serializes and sets Content-Type header
                request.AddJsonBody(new
                {
                    grant_type = "authorization_code",
                    code = oauthCode,
                    client_id = clientId,
                    client_secret = clientSecret
                });

                // RestSharp: Execute POST request
                var response = await client.ExecutePostAsync(request);

                // RestSharp: Check if request was successful
                if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                {
                    _logger.LogError("Failed to get access token from Amazon. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, response.Content);
                    return null;
                }

                // Deserialize Amazon's JSON response
                var tokenData = JsonConvert.DeserializeObject<dynamic>(response.Content);
                if (tokenData == null)
                    return null;

                return new AccessTokenResponse
                {
                    AccessToken = tokenData.access_token,
                    RefreshToken = tokenData.refresh_token,
                    ExpiresIn = tokenData.expires_in
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access token from Amazon");
                return null;
            }
        }

        public async Task<ApiResponse<List<SubscriptionListItem>>> GetAllSubscriptionsAsync(string orgId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_masterConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT
                        sub_id,
                        sub_name,
                        old_data_load,
                        country,
                        COALESCE(country, '') as country_code,
                        0 as priority
                    FROM seller_subscriptions
                    WHERE org_id = @org_id AND is_active = true
                    ORDER BY last_updated DESC;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@org_id", Guid.Parse(orgId));

                var subscriptions = new List<SubscriptionListItem>();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    subscriptions.Add(new SubscriptionListItem
                    {
                        SubId = reader.GetGuid(reader.GetOrdinal("sub_id")),
                        SubName = reader.IsDBNull(reader.GetOrdinal("sub_name")) ? string.Empty : reader.GetString(reader.GetOrdinal("sub_name")),
                        OldDataLoad = reader.GetBoolean(reader.GetOrdinal("old_data_load")),
                        Country = reader.IsDBNull(reader.GetOrdinal("country")) ? string.Empty : reader.GetString(reader.GetOrdinal("country")),
                        CountryCode = reader.IsDBNull(reader.GetOrdinal("country_code")) ? string.Empty : reader.GetString(reader.GetOrdinal("country_code")),
                        Priority = reader.GetInt32(reader.GetOrdinal("priority"))
                    });
                }

                _logger.LogInformation("Retrieved {Count} subscriptions for OrgId: {OrgId}", subscriptions.Count, orgId);
                return ApiResponse<List<SubscriptionListItem>>.Ok(subscriptions, "Subscriptions retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions for OrgId: {OrgId}", orgId);
                return ApiResponse<List<SubscriptionListItem>>.Fail($"Error retrieving subscriptions: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<SubscriptionDetail>>> GetSubscriptionDetailsAsync(string orgId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_masterConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT
                        sub_id,
                        region,
                        country,
                        sub_name,
                        COALESCE(selling_partner_id, '') as selling_partner_id,
                        COALESCE(sub_name, '') as account_name,
                        old_data_load,
                        0 as priority
                    FROM seller_subscriptions
                    WHERE org_id = @org_id
                    ORDER BY last_updated DESC;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@org_id", Guid.Parse(orgId));

                var details = new List<SubscriptionDetail>();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    details.Add(new SubscriptionDetail
                    {
                        SubId = reader.GetGuid(reader.GetOrdinal("sub_id")),
                        Region = reader.IsDBNull(reader.GetOrdinal("region")) ? string.Empty : reader.GetString(reader.GetOrdinal("region")),
                        Country = reader.IsDBNull(reader.GetOrdinal("country")) ? string.Empty : reader.GetString(reader.GetOrdinal("country")),
                        SubName = reader.IsDBNull(reader.GetOrdinal("sub_name")) ? string.Empty : reader.GetString(reader.GetOrdinal("sub_name")),
                        SellingPartnerId = reader.IsDBNull(reader.GetOrdinal("selling_partner_id")) ? string.Empty : reader.GetString(reader.GetOrdinal("selling_partner_id")),
                        AccountName = reader.IsDBNull(reader.GetOrdinal("account_name")) ? string.Empty : reader.GetString(reader.GetOrdinal("account_name")),
                        OldDataLoad = reader.GetBoolean(reader.GetOrdinal("old_data_load")),
                        Priority = reader.GetInt32(reader.GetOrdinal("priority"))
                    });
                }

                _logger.LogInformation("Retrieved {Count} subscription details for OrgId: {OrgId}", details.Count, orgId);
                return ApiResponse<List<SubscriptionDetail>>.Ok(details, "Subscription details retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription details for OrgId: {OrgId}", orgId);
                return ApiResponse<List<SubscriptionDetail>>.Fail($"Error retrieving subscription details: {ex.Message}");
            }
        }

        public async Task<ApiResponse<SellerSubscription>> GetSubscriptionByIdAsync(string orgId, Guid subId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_masterConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT *
                    FROM seller_subscriptions
                    WHERE sub_id = @sub_id AND org_id = @org_id;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@sub_id", subId);
                cmd.Parameters.AddWithValue("@org_id", Guid.Parse(orgId));

                await using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    _logger.LogWarning("Subscription not found. SubId: {SubId}, OrgId: {OrgId}", subId, orgId);
                    return ApiResponse<SellerSubscription>.Fail("Subscription not found");
                }

                var subscription = new SellerSubscription
                {
                    SubId = reader.GetGuid(reader.GetOrdinal("sub_id")),
                    OrgId = reader.GetGuid(reader.GetOrdinal("org_id")),
                    SubName = reader.IsDBNull(reader.GetOrdinal("sub_name")) ? null : reader.GetString(reader.GetOrdinal("sub_name")),
                    Region = reader.IsDBNull(reader.GetOrdinal("region")) ? null : reader.GetString(reader.GetOrdinal("region")),
                    Country = reader.IsDBNull(reader.GetOrdinal("country")) ? null : reader.GetString(reader.GetOrdinal("country")),
                    MarketplaceId = reader.IsDBNull(reader.GetOrdinal("marketplace_id")) ? null : reader.GetString(reader.GetOrdinal("marketplace_id")),
                    SellingPartnerId = reader.IsDBNull(reader.GetOrdinal("selling_partner_id")) ? null : reader.GetString(reader.GetOrdinal("selling_partner_id")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    OldDataLoad = reader.GetBoolean(reader.GetOrdinal("old_data_load")),
                    LoadingInProgress = reader.GetBoolean(reader.GetOrdinal("loading_in_progress"))
                };

                _logger.LogInformation("Retrieved subscription. SubId: {SubId}, OrgId: {OrgId}", subId, orgId);
                return ApiResponse<SellerSubscription>.Ok(subscription, "Subscription retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription. SubId: {SubId}, OrgId: {OrgId}", subId, orgId);
                return ApiResponse<SellerSubscription>.Fail($"Error retrieving subscription: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<string>>> GetAllCountriesAsync(string orgId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_masterConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT DISTINCT country
                    FROM seller_subscriptions
                    WHERE org_id = @org_id AND country IS NOT NULL
                    ORDER BY country;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@org_id", Guid.Parse(orgId));

                var countries = new List<string>();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var country = reader.GetString(reader.GetOrdinal("country"));
                    countries.Add(country);
                }

                _logger.LogInformation("Retrieved {Count} countries for OrgId: {OrgId}", countries.Count, orgId);
                return ApiResponse<List<string>>.Ok(countries, "Countries retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting countries for OrgId: {OrgId}", orgId);
                return ApiResponse<List<string>>.Fail($"Error retrieving countries: {ex.Message}");
            }
        }

        // ============================================================================
        // COMMENTED OUT - GetMarketplacesByRegionAsync (uses tbl_marketplace_region)
        // ============================================================================
        // This method queries tbl_marketplace_region which may not exist yet
        // Uncomment when the table is ready in the master database
        //
        // public async Task<ApiResponse<List<RegionalMarketplace>>> GetMarketplacesByRegionAsync(string region)
        // {
        //     try
        //     {
        //         await using var conn = new NpgsqlConnection(_masterConnectionString);
        //         await conn.OpenAsync();
        //
        //         var sql = @"
        //             SELECT country, marketplace_id, gmt
        //             FROM tbl_marketplace_region
        //             WHERE region = @region
        //             ORDER BY country;";
        //
        //         await using var cmd = new NpgsqlCommand(sql, conn);
        //         cmd.Parameters.AddWithValue("@region", region);
        //
        //         var marketplaces = new List<RegionalMarketplace>();
        //         await using var reader = await cmd.ExecuteReaderAsync();
        //
        //         while (await reader.ReadAsync())
        //         {
        //             marketplaces.Add(new RegionalMarketplace
        //             {
        //                 Country = reader.IsDBNull(reader.GetOrdinal("country")) ? string.Empty : reader.GetString(reader.GetOrdinal("country")),
        //                 MarketplaceId = reader.IsDBNull(reader.GetOrdinal("marketplace_id")) ? string.Empty : reader.GetString(reader.GetOrdinal("marketplace_id")),
        //                 Gmt = reader.IsDBNull(reader.GetOrdinal("gmt")) ? string.Empty : reader.GetString(reader.GetOrdinal("gmt"))
        //             });
        //         }
        //
        //         _logger.LogInformation("Retrieved {Count} marketplaces for region: {Region}", marketplaces.Count, region);
        //         return ApiResponse<List<RegionalMarketplace>>.Ok(marketplaces, "Marketplaces retrieved successfully");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error getting marketplaces for region: {Region}", region);
        //         return ApiResponse<List<RegionalMarketplace>>.Fail($"Error retrieving marketplaces: {ex.Message}");
        //     }
        // }
        // ============================================================================

        /// <summary>
        /// Temporary stub method - GetMarketplacesByRegionAsync
        /// Returns empty list until tbl_marketplace_region table is implemented
        /// </summary>
        public async Task<ApiResponse<List<RegionalMarketplace>>> GetMarketplacesByRegionAsync(string region)
        {
            _logger.LogWarning("GetMarketplacesByRegionAsync called but tbl_marketplace_region table not implemented yet. Region: {Region}", region);
            await Task.CompletedTask; // Keep async signature
            return ApiResponse<List<RegionalMarketplace>>.Ok(new List<RegionalMarketplace>(),
                "Marketplace data not available - tbl_marketplace_region table not implemented");
        }

        public async Task<bool> SubscriptionExistsAsync(string orgId, string sellingPartnerId, string mwsAuthToken)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_masterConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT COUNT(*)
                    FROM seller_subscriptions
                    WHERE org_id = @org_id
                      AND selling_partner_id = @selling_partner_id
                      AND mws_auth_token = @mws_auth_token;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@org_id", Guid.Parse(orgId));
                cmd.Parameters.AddWithValue("@selling_partner_id", sellingPartnerId);
                cmd.Parameters.AddWithValue("@mws_auth_token", mwsAuthToken);

                var count = (long)await cmd.ExecuteScalarAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription existence for OrgId: {OrgId}", orgId);
                return false;
            }
        }
    }
}
