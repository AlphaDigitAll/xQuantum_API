using Npgsql;
using NpgsqlTypes;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Interfaces.Tenant;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Services.Reports
{
    /// <summary>
    /// ULTRA-OPTIMIZED Service for sales graph aggregate data
    /// Returns JSON directly from PostgreSQL with ZERO C# conversion overhead
    /// Database does all aggregation and formatting - C# just passes it through
    /// </summary>
    public class SalesGraphService : TenantAwareServiceBase, ISalesGraphService
    {
        public SalesGraphService(
            ILogger<SalesGraphService> logger,
            ITenantService tenantService,
            IConnectionStringManager connectionManager)
            : base(connectionManager, tenantService, logger)
        {
        }

        /// <summary>
        /// Super-fast retrieval - Returns aggregated graph data as JSON directly from database
        /// NO deserialization, NO re-serialization, NO C# object mapping
        /// Pure passthrough for maximum performance
        /// </summary>
        public async Task<string> GetSalesGraphAggregatesJsonAsync(string orgId, GraphFilterRequest request)
        {
            if (request.SubId == Guid.Empty)
                return BuildErrorJson("SubId is required.");
            if (string.IsNullOrWhiteSpace(request.ChartName))
                return BuildErrorJson("ChartName is required (hour, day, week, month, date).");
            if (string.IsNullOrWhiteSpace(request.TabType))
                return BuildErrorJson("TabType is required (e.g., order, business, inventory).");
            var validCharts = new[] { "hour", "day", "week", "month", "date" };
            if (!validCharts.Contains(request.ChartName.ToLower()))
                return BuildErrorJson($"Invalid ChartName. Must be one of: {string.Join(", ", validCharts)}");

            try
            {
                var result = await ExecuteTenantOperation(orgId, async conn =>
                {
                    const string sql = @"
                SELECT fn_amz_get_seller_sales_graph(
                    @p_load_type,
                    @p_chart_name,
                    @p_sub_id,
                    @p_from_date,
                    @p_to_date,
                    @p_filters
                );";

                    await using var cmd = new NpgsqlCommand(sql, conn);

                    cmd.Parameters.AddWithValue("@p_load_type", request.TabType.ToLower());
                    cmd.Parameters.AddWithValue("@p_chart_name", request.ChartName.ToLower());
                    cmd.Parameters.AddWithValue("@p_sub_id", request.SubId);
                    cmd.Parameters.Add("@p_from_date", NpgsqlTypes.NpgsqlDbType.Date).Value = (object?)request.FromDate ?? DBNull.Value;
                    cmd.Parameters.Add("@p_to_date", NpgsqlTypes.NpgsqlDbType.Date).Value = (object?)request.ToDate ?? DBNull.Value;
                    var filtersJson = System.Text.Json.JsonSerializer.Serialize(request.Filters ?? new Dictionary<string, string>());
                    cmd.Parameters.Add(new NpgsqlParameter("@p_filters", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = filtersJson });

                    var jsonResult = await cmd.ExecuteScalarAsync();

                    if (jsonResult == null || jsonResult == DBNull.Value)
                        return BuildErrorJson("No data returned from database.");

                    return jsonResult.ToString() ?? BuildErrorJson("Failed to retrieve data.");
                }, $"Get Sales Graph Aggregates - {request.TabType}/{request.ChartName}");

                return result.Success ? result.Data! : BuildErrorJson(result.Message);
            }
            catch (PostgresException pgEx)
            {
                Logger.LogError(pgEx, "PostgreSQL error while fetching sales graph aggregates for OrgId: {OrgId}, Module: {Module}, Chart: {Chart}", orgId, request.TabType, request.ChartName);
                return BuildErrorJson($"Database error: {pgEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error while fetching sales graph aggregates for OrgId: {OrgId}, Module: {Module}, Chart: {Chart}", orgId, request.TabType, request.ChartName);
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
