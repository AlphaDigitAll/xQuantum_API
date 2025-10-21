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

            var response = await ExecuteTenantOperation(orgId, async conn =>
            {
                const string sql = @"SELECT public.fn_amz_get_seller_sales_summary(
                 @p_load_type, @p_load_level, @p_sub_id, @p_from_date, @p_to_date,
                 @p_page, @p_page_size, @p_sort_field, @p_sort_order, @p_global_search, @p_filters
                 );";

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.Add("@p_load_type", NpgsqlTypes.NpgsqlDbType.Text).Value = request.TabType.ToLower();
                cmd.Parameters.Add("@p_load_level", NpgsqlTypes.NpgsqlDbType.Text).Value = request.LoadLevel.ToLower();
                cmd.Parameters.Add("@p_sub_id", NpgsqlTypes.NpgsqlDbType.Uuid).Value = request.SubId;
                cmd.Parameters.Add("@p_from_date", NpgsqlTypes.NpgsqlDbType.Date).Value = (object?)request.FromDate ?? DBNull.Value;
                cmd.Parameters.Add("@p_to_date", NpgsqlTypes.NpgsqlDbType.Date).Value = (object?)request.ToDate ?? DBNull.Value;
                cmd.Parameters.Add("@p_page", NpgsqlTypes.NpgsqlDbType.Integer).Value = request.Page;
                cmd.Parameters.Add("@p_page_size", NpgsqlTypes.NpgsqlDbType.Integer).Value = request.PageSize;
                cmd.Parameters.Add("@p_sort_field", NpgsqlTypes.NpgsqlDbType.Text).Value = request.SortField ?? "order_date";
                cmd.Parameters.Add("@p_sort_order", NpgsqlTypes.NpgsqlDbType.Text).Value = request.SortOrder ?? "DESC";
                cmd.Parameters.Add("@p_global_search", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)request.GlobalSearch ?? DBNull.Value;


                var json = "{}";
                if (request.Filters != null)
                    json = System.Text.Json.JsonSerializer.Serialize(request.Filters, typeof(object));

                cmd.Parameters.Add("@p_filters", NpgsqlTypes.NpgsqlDbType.Jsonb).Value = json;

                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? BuildErrorJson("No data returned.");

            }, $"Get Seller Summary ({request.TabType}/{request.LoadLevel})");
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
