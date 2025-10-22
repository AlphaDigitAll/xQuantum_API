using Npgsql;
using NpgsqlTypes;
using xQuantum_API.Helpers;
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
            // Validate required fields
            if (request == null)
                return JsonResponseBuilder.BuildErrorJson("Request body is required.");

            if (request.SubId == Guid.Empty)
                return JsonResponseBuilder.BuildErrorJson("SubId is required.");

            if (string.IsNullOrWhiteSpace(request.ChartName))
                return JsonResponseBuilder.BuildErrorJson("ChartName is required (hour, day, week, month, date).");

            if (request.TabType == 0)
                return JsonResponseBuilder.BuildErrorJson("TabType is required.");

            if (string.IsNullOrWhiteSpace(orgId))
                return JsonResponseBuilder.BuildErrorJson("OrgId is missing.");

            // Validate date range business rule
            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value > request.ToDate.Value)
                return JsonResponseBuilder.BuildErrorJson("FromDate cannot be greater than ToDate.");

            // Log the operation
            Logger.LogInformation(
                "Fetching sales graph aggregates for OrgId: {OrgId}, SubId: {SubId}, Module: {Module}, Level: {Level}",
                orgId, request.SubId, request.TabType, request.ChartName);

            try
            {
                string functionName = request.TabType switch
                {
                    2 => "public.fn_amz_get_seller_sales_summary_product_graph",  
                    3 => "public.fn_amz_get_seller_sales_summary_demographic_graph", 
                    4 => "public.fn_amz_get_seller_sales_summary_shipping_graph",    
                    5 => "public.fn_amz_get_seller_sales_summary_promotion_graph", 
                    _ => "public.fn_amz_get_seller_sales_summary_graph"
                };

                var result = await ExecuteTenantOperation(orgId, async conn =>
                {
                    var sql = $@"
                SELECT {functionName}(
                    @p_load_type,
                    @p_chart_name,
                    @p_sub_id,
                    @p_from_date,
                    @p_to_date,
                    @p_filters
                );";

                    await using var cmd = new NpgsqlCommand(sql, conn);

                    cmd.Parameters.AddWithValue("@p_load_type", NpgsqlTypes.NpgsqlDbType.Text, request.LoadTypeText);
                    cmd.Parameters.AddWithValue("@p_chart_name", NpgsqlTypes.NpgsqlDbType.Text, request.ChartName.ToLower());
                    cmd.Parameters.AddWithValue("@p_sub_id", NpgsqlTypes.NpgsqlDbType.Uuid, request.SubId);
                    cmd.Parameters.AddWithValue("@p_from_date", NpgsqlTypes.NpgsqlDbType.Timestamp, (object?)request.FromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_to_date", NpgsqlTypes.NpgsqlDbType.Timestamp, (object?)request.ToDate ?? DBNull.Value);

                    var filtersJson = System.Text.Json.JsonSerializer.Serialize(request.Filters ?? new Dictionary<string, string>());
                    cmd.Parameters.Add(new NpgsqlParameter("@p_filters", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = filtersJson });

                    var jsonResult = await cmd.ExecuteScalarAsync();

                    if (jsonResult == null || jsonResult == DBNull.Value)
                        return JsonResponseBuilder.BuildErrorJson("No data returned from database.");

                    return jsonResult.ToString() ?? JsonResponseBuilder.BuildErrorJson("Failed to retrieve data.");
                }, $"Get Sales Graph Aggregates - {request.TabType}/{request.ChartName}");

                return result.Success ? result.Data! : JsonResponseBuilder.BuildErrorJson(result.Message);
            }
            catch (PostgresException pgEx)
            {
                Logger.LogError(pgEx, "PostgreSQL error while fetching sales graph aggregates for OrgId: {OrgId}, Module: {Module}, Chart: {Chart}", orgId, request.TabType, request.ChartName);
                return JsonResponseBuilder.BuildErrorJson($"Database error: {pgEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error while fetching sales graph aggregates for OrgId: {OrgId}, Module: {Module}, Chart: {Chart}", orgId, request.TabType, request.ChartName);
                return JsonResponseBuilder.BuildErrorJson($"Unexpected error: {ex.Message}");
            }
        }

    }
}
