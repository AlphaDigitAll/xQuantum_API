using Newtonsoft.Json;
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

        public async Task<string> GetSellerSalesSummaryJsonAsync(string orgId, SummaryFilterRequest request)
        {
            if (request.SubId == Guid.Empty)
                return BuildErrorJson("SubId is required.");

            // 🔹 Dynamic function routing
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
                cmd.Parameters.AddWithValue("@p_load_type", NpgsqlTypes.NpgsqlDbType.Text, request.TableName);
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
                return result?.ToString() ?? BuildErrorJson("No data returned.");
            },
            $"Get Seller Summary ({request.TableName}/{request.TableName})");

            return response.Success ? response.Data ?? BuildErrorJson("Empty data") : BuildErrorJson(response.Message);
        }

        public async Task<string> GetSalesHeatmapJsonAsync(string orgId, SalesHeatmapRequest request)
        {
            if (request.SubId == Guid.Empty)
                return BuildErrorJson("SubId is required.");

            if (request.FromDate == default || request.ToDate == default)
                return BuildErrorJson("FromDate and ToDate are required.");

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
                return result?.ToString() ?? BuildErrorJson("No data returned.");

            }, $"Get Sales Heatmap ({request.TabType})");

            return response.Success ? response.Data ?? BuildErrorJson("Empty data") : BuildErrorJson(response.Message);
        }

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
