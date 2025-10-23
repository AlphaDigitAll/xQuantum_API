using Newtonsoft.Json;
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

        public async Task<string> GetSalesSummaryCardsJsonAsync(string orgId, SummaryCardRequest request)
        {
            if (request.SubId == Guid.Empty)
                return JsonResponseBuilder.BuildErrorJson("SubId is required.");
            if (request.FromDate == null || request.ToDate == null)
                return JsonResponseBuilder.BuildErrorJson("FromDate and ToDate are required.");

            try
            {

                var result = await ExecuteTenantOperation(orgId, async conn =>
                {
                    var sql = $@"
                SELECT fn_amz_get_seller_sales_summary_cards(
                    @p_sub_id,
                    @p_from_date,
                    @p_to_date
                );";

                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p_sub_id", NpgsqlTypes.NpgsqlDbType.Uuid, request.SubId);
                    cmd.Parameters.AddWithValue("@p_from_date", NpgsqlTypes.NpgsqlDbType.Date, (object?)request.FromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_to_date", NpgsqlTypes.NpgsqlDbType.Date, (object?)request.ToDate ?? DBNull.Value);
                    var jsonResult = await cmd.ExecuteScalarAsync();

                    if (jsonResult == null || jsonResult == DBNull.Value)
                        return JsonResponseBuilder.BuildErrorJson("No data returned from database.");

                    return jsonResult.ToString() ?? JsonResponseBuilder.BuildErrorJson("Failed to retrieve data.");
                }, $"Get Sales Summary Cards - {request.SubId}");

                return result.Success ? result.Data! : JsonResponseBuilder.BuildErrorJson(result.Message);
            }
            catch (PostgresException pgEx)
            {
                Logger.LogError(pgEx,
                    "PostgreSQL error while fetching sales summary cards for OrgId: {OrgId}, SubId: {SubId}",
                    orgId, request.SubId);
                return JsonResponseBuilder.BuildErrorJson($"Database error: {pgEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Unexpected error while fetching sales summary cards for OrgId: {OrgId}, SubId: {SubId}",
                    orgId, request.SubId);
                return JsonResponseBuilder.BuildErrorJson($"Unexpected error: {ex.Message}");
            }
        }

        public async Task<string> GetSellerSalesSummaryJsonAsync(string orgId, SummaryFilterRequest request)
        {
            // Validate required fields
            if (request == null)
                return JsonResponseBuilder.BuildErrorJson("Request is required.");

            if (request.SubId == Guid.Empty)
                return JsonResponseBuilder.BuildErrorJson("SubId is required.");

            if (string.IsNullOrWhiteSpace(request.TableName))
                return JsonResponseBuilder.BuildErrorJson("TableName is required.");

            // Normalize and apply business rules
            request.Page = Math.Max(request.Page, 1); // Ensure page >= 1
            request.PageSize = (request.PageSize <= 0 || request.PageSize > 1000) ? 100 : request.PageSize; // Default to 100, max 1000
            request.TableName = request.TableName.ToLowerInvariant(); // Normalize to lowercase

            string functionName = request.TabType switch
            {
                2 => "public.fn_amz_get_seller_sales_summary_product",
                3 => "public.fn_amz_get_seller_sales_summary_demographic",
                4 => "public.fn_amz_get_seller_sales_summary_shipping",
                5=> "public.fn_amz_get_seller_sales_summary_promotion",
                _ => "public.fn_amz_get_seller_sales_summary_date_and_time" 
            };

            var response = await ExecuteTenantOperation(orgId, async conn =>
            {
                var sql = $@"
            SELECT {functionName}(
                @p_load_type,
                @p_load_level,
                @p_sub_id,
                @p_from_date,
                @p_to_date,
                @p_page,
                @p_page_size,
                @p_sort_field,
                @p_sort_order,
                @p_global_search,
                @p_filters
            );";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p_load_type", NpgsqlTypes.NpgsqlDbType.Text, request.LoadTypeText);
                cmd.Parameters.AddWithValue("@p_load_level", NpgsqlTypes.NpgsqlDbType.Text, request.TableName?.ToLower() ?? "date");
                cmd.Parameters.AddWithValue("@p_sub_id", NpgsqlTypes.NpgsqlDbType.Uuid, request.SubId);
                cmd.Parameters.AddWithValue("@p_from_date", NpgsqlTypes.NpgsqlDbType.Date, (object?)request.FromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p_to_date", NpgsqlTypes.NpgsqlDbType.Date, (object?)request.ToDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p_page", NpgsqlTypes.NpgsqlDbType.Integer, request.Page);
                cmd.Parameters.AddWithValue("@p_page_size", NpgsqlTypes.NpgsqlDbType.Integer, request.PageSize);
                cmd.Parameters.AddWithValue("@p_sort_field", NpgsqlTypes.NpgsqlDbType.Text, request.SortField ?? "order_date");
                cmd.Parameters.AddWithValue("@p_sort_order", NpgsqlTypes.NpgsqlDbType.Text, request.SortOrder ?? "DESC");
                cmd.Parameters.AddWithValue("@p_global_search", NpgsqlTypes.NpgsqlDbType.Text, (object?)request.GlobalSearch ?? DBNull.Value);

                var jsonFilters = request.Filters != null
                    ? System.Text.Json.JsonSerializer.Serialize(request.Filters)
                    : "{}";
                cmd.Parameters.AddWithValue("@p_filters", NpgsqlTypes.NpgsqlDbType.Jsonb, jsonFilters);

                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? JsonResponseBuilder.BuildErrorJson("No data returned.");
            },
            $"Get Seller Summary ({request.TableName}/{request.TableName})");

            return response.Success ? response.Data ?? JsonResponseBuilder.BuildErrorJson("Empty data") : JsonResponseBuilder.BuildErrorJson(response.Message);
        }
        public async Task<string> GetSalesHeatmapJsonAsync(string orgId, SalesHeatmapRequest request)
        {
            if (request.SubId == Guid.Empty)
                return JsonResponseBuilder.BuildErrorJson("SubId is required.");

            if (request.FromDate == default || request.ToDate == default)
                return JsonResponseBuilder.BuildErrorJson("FromDate and ToDate are required.");

            var response = await ExecuteTenantOperation(orgId, async conn =>
            {
                const string sql = @"SELECT public.fn_get_sales_heatmap(
                    @p_tab_type, @p_sub_id, @p_from_date, @p_to_date
                );";

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.Add("@p_tab_type", NpgsqlDbType.Integer).Value = (int)request.TabType;
                cmd.Parameters.Add("@p_sub_id", NpgsqlDbType.Uuid).Value = request.SubId;
                cmd.Parameters.Add("@p_from_date", NpgsqlDbType.Timestamp).Value = request.FromDate;
                cmd.Parameters.Add("@p_to_date", NpgsqlDbType.Timestamp).Value = request.ToDate;

                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? JsonResponseBuilder.BuildErrorJson("No data returned.");

            }, $"Get Sales Heatmap ({request.TabType})");

            return response.Success ? response.Data ?? JsonResponseBuilder.BuildErrorJson("Empty data") : JsonResponseBuilder.BuildErrorJson(response.Message);
        }

    }
}
