using Npgsql;
using NpgsqlTypes;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Interfaces.Tenant;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Services.Reports
{
    /// <summary>
    /// ULTRA-OPTIMIZED Service for sales summary data
    /// Returns JSON directly from PostgreSQL with ZERO C# conversion overhead
    /// Database does all the heavy lifting - C# just passes it through
    /// </summary>
    public class SalesSummaryService : TenantAwareServiceBase, ISalesSummaryService
    {
        public SalesSummaryService(
            ILogger<SalesSummaryService> logger,
            ITenantService tenantService,
            IConnectionStringManager connectionManager)
            : base(connectionManager, tenantService, logger)
        {
        }

        /// <summary>
        /// Super-fast retrieval - Returns JSON directly from database
        /// NO deserialization, NO re-serialization, NO C# object mapping
        /// Pure passthrough for maximum performance
        /// </summary>
        public async Task<string> GetSalesSummaryJsonAsync(
            string orgId,
            SummaryFilterRequest request)
        {
            // Validate required parameters
            if (request.SubId == Guid.Empty)
            {
                return BuildErrorJson("SubId is required.");
            }

            if (string.IsNullOrWhiteSpace(request.LoadLevel))
            {
                return BuildErrorJson("LoadLevel is required (day, week, month, or hour).");
            }

            if (string.IsNullOrWhiteSpace(request.TabType))
            {
                return BuildErrorJson("TabType is required (e.g., order, business, inventory).");
            }

            // Validate LoadLevel values
            var validLoadLevels = new[] { "day", "week", "month", "hour" };
            if (!validLoadLevels.Contains(request.LoadLevel.ToLower()))
            {
                return BuildErrorJson($"Invalid LoadLevel. Must be one of: {string.Join(", ", validLoadLevels)}");
            }

            try
            {
                var result = await ExecuteTenantOperation(orgId, async conn =>
                {
                    // Call the optimized PostgreSQL function
                    const string sql = @"
                        SELECT fn_get_summary_single_table_filtered_v2(
                            @p_module,
                            @p_load_level,
                            @p_sub_id,
                            @p_filters,
                            @p_from_date,
                            @p_to_date,
                            @p_offset,
                            @p_limit
                        )";

                    await using var cmd = new NpgsqlCommand(sql, conn);

                    // Add parameters
                    cmd.Parameters.AddWithValue("@p_module", request.TabType.ToLower());
                    cmd.Parameters.AddWithValue("@p_load_level", request.LoadLevel.ToLower());
                    cmd.Parameters.AddWithValue("@p_sub_id", request.SubId);

                    // Convert filters to JSONB - this is the ONLY conversion we do
                    var filtersJson = System.Text.Json.JsonSerializer.Serialize(request.Filters ?? new Dictionary<string, string>());
                    cmd.Parameters.Add(new NpgsqlParameter("@p_filters", NpgsqlDbType.Jsonb) { Value = filtersJson });

                    cmd.Parameters.AddWithValue("@p_from_date",
                        request.FromDate.HasValue ? (object)request.FromDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_to_date",
                        request.ToDate.HasValue ? (object)request.ToDate.Value : DBNull.Value);

                    cmd.Parameters.AddWithValue("@p_offset", request.Offset);
                    cmd.Parameters.AddWithValue("@p_limit", request.Limit);

                    // Get JSON directly from database - NO CONVERSION!
                    var jsonResult = await cmd.ExecuteScalarAsync();

                    if (jsonResult == null || jsonResult == DBNull.Value)
                    {
                        return BuildErrorJson("No data returned from database.");
                    }

                    // Return raw JSON string - database already formatted everything perfectly!
                    return jsonResult.ToString() ?? BuildErrorJson("Failed to retrieve data.");

                }, $"Get Sales Summary - {request.TabType}/{request.LoadLevel}");

                return result.Success ? result.Data! : BuildErrorJson(result.Message);
            }
            catch (PostgresException pgEx)
            {
                Logger.LogError(pgEx,
                    "PostgreSQL error while fetching sales summary for OrgId: {OrgId}, Module: {Module}, Level: {Level}",
                    orgId, request.TabType, request.LoadLevel);

                return BuildErrorJson($"Database error: {pgEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Unexpected error while fetching sales summary for OrgId: {OrgId}, Module: {Module}, Level: {Level}",
                    orgId, request.TabType, request.LoadLevel);

                return BuildErrorJson($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Build error response in JSON format (matches PostgreSQL function output)
        /// </summary>
        private string BuildErrorJson(string message)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                message = message,
                data = (object?)null
            });
        }

    }
}
