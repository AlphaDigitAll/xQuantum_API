using Newtonsoft.Json;
using Npgsql;
using xQuantum_API.Helpers;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Interfaces.Tenant;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Services.Reports
{
    public class InventoryService : TenantAwareServiceBase, IInventoryService
    {
        public InventoryService(
            ILogger<InventoryService> logger,
            ITenantService tenantService,
            IConnectionStringManager connectionManager)
            : base(connectionManager, tenantService, logger)
        {
        }

        public async Task<ApiResponse<InventoryCardSummary>> GetInventoryCardAsync(string orgId, Guid subId)
        {
            try
            {
                return await ExecuteTenantOperation(orgId, async conn =>
            {
                const string sql = "SELECT * FROM public.fn_amz_inventory_card_summary(@subId)";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@subId", subId);

                await using var sdr = await cmd.ExecuteReaderAsync();

                InventoryCardSummary summary = null!;

                if (await sdr.ReadAsync())
                {
                    var summaryObj = new InventoryCardSummary
                    {
                        sub_id = sdr.GetGuid(sdr.GetOrdinal("sub_id")),
                        Fulfillable_Quantity = new FulfillableGroup
                        {
                            fulfillable_quantity = sdr.GetInt32(sdr.GetOrdinal("fulfillable_quantity")),
                            working_quantity = sdr.GetInt32(sdr.GetOrdinal("working_quantity")),
                            shipped_quantity = sdr.GetInt32(sdr.GetOrdinal("shipped_quantity")),
                            receiving_quantity = sdr.GetInt32(sdr.GetOrdinal("receiving_quantity"))
                        },
                        Reserved_Quantity = new ReservedGroup
                        {
                            total_reserved_quantity = sdr.GetInt32(sdr.GetOrdinal("total_reserved_quantity")),
                            customer_order_quantity = sdr.GetInt32(sdr.GetOrdinal("customer_order_quantity")),
                            trans_shipment_quantity = sdr.GetInt32(sdr.GetOrdinal("trans_shipment_quantity")),
                            fc_processing_quantity = sdr.GetInt32(sdr.GetOrdinal("fc_processing_quantity"))
                        },
                        Unfulfillable_Quantity = new UnfulfillableGroup
                        {
                            unfulfillable_quantity = sdr.GetInt32(sdr.GetOrdinal("unfulfillable_quantity")),
                            customer_damaged_quantity = sdr.GetInt32(sdr.GetOrdinal("customer_damaged_quantity")),
                            warehouse_damaged_quantity = sdr.GetInt32(sdr.GetOrdinal("warehouse_damaged_quantity")),
                            distributor_damaged_quantity = sdr.GetInt32(sdr.GetOrdinal("distributor_damaged_quantity")),
                            carrier_damaged_quantity = sdr.GetInt32(sdr.GetOrdinal("carrier_damaged_quantity")),
                            defective_quantity = sdr.GetInt32(sdr.GetOrdinal("defective_quantity")),
                            expired_quantity = sdr.GetInt32(sdr.GetOrdinal("expired_quantity"))
                        }
                    };
                }
                return summary ?? new InventoryCardSummary { sub_id = subId };
            },
            "Get Inventory Card Summary");
            }
            catch (NpgsqlException npgEx)
            {
                return new ApiResponse<InventoryCardSummary>
                {
                    Success = false,
                    Message = $"Database error while fetching inventory summary: {npgEx.Message}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<InventoryCardSummary>
                {
                    Success = false,
                    Message = $"Unexpected error occurred: {ex.Message}",
                    Data = null
                };
            }
        }
        public async Task<string> GetInventoryJsonAsync(string orgId, InventoryQueryRequest request)
        {
            if (request.Page <= 0) return JsonResponseBuilder.BuildErrorJson("Page must be greater than 0.");
            if (request.PageSize <= 0) return JsonResponseBuilder.BuildErrorJson("PageSize must be greater than 0.");
            if (string.IsNullOrEmpty(request.TabType)) return JsonResponseBuilder.BuildErrorJson("TabType is required.");

            try
            {
                var result = await ExecuteTenantOperation(orgId, async conn =>
                {
                    var sql = @"SELECT public.fn_amz_get_inventory(
                            @p_page, @p_page_size, @p_sort_field, @p_sort_order,
                            @p_global_search, @p_tab_type, @p_filters);";

                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p_page", NpgsqlTypes.NpgsqlDbType.Integer, request.Page);
                    cmd.Parameters.AddWithValue("@p_page_size", NpgsqlTypes.NpgsqlDbType.Integer, request.PageSize);
                    cmd.Parameters.AddWithValue("@p_sort_field", NpgsqlTypes.NpgsqlDbType.Text, (object?)request.SortField ?? "asin");
                    cmd.Parameters.AddWithValue("@p_sort_order", NpgsqlTypes.NpgsqlDbType.Integer, request.SortOrder);
                    cmd.Parameters.AddWithValue("@p_global_search", NpgsqlTypes.NpgsqlDbType.Text, (object?)request.GlobalSearch ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_tab_type", NpgsqlTypes.NpgsqlDbType.Text, (object?)request.TabType ?? "product");
                    var filtersJson = request.Filters != null && request.Filters.Any()? System.Text.Json.JsonSerializer.Serialize(request.Filters): "{}";
                    cmd.Parameters.AddWithValue("@p_filters", NpgsqlTypes.NpgsqlDbType.Jsonb, filtersJson);

                    var jsonResult = await cmd.ExecuteScalarAsync();
                    if (jsonResult == null || jsonResult == DBNull.Value)
                        return JsonResponseBuilder.BuildErrorJson("No data returned from fn_get_inventory.");

                    return jsonResult.ToString() ?? JsonResponseBuilder.BuildErrorJson("Empty JSON response.");
                }, $"Get Inventory - {request.TabType}");

                return result.Success ? result.Data! : JsonResponseBuilder.BuildErrorJson(result.Message);
            }
            catch (PostgresException ex)
            {
                Logger.LogError(ex, "PostgreSQL error for OrgId: {OrgId}, TabType: {TabType}", orgId, request.TabType);
                return JsonResponseBuilder.BuildErrorJson($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error for OrgId: {OrgId}, TabType: {TabType}", orgId, request.TabType);
                return JsonResponseBuilder.BuildErrorJson($"Unexpected error: {ex.Message}");
            }
        }
        public async Task<string> GetInventorySalesJsonAsync(string orgId, InventoryQueryRequest request)
        {
            if (request.Page <= 0) return JsonResponseBuilder.BuildErrorJson("Page must be greater than 0.");
            if (request.PageSize <= 0) return JsonResponseBuilder.BuildErrorJson("PageSize must be greater than 0.");
            if (string.IsNullOrEmpty(request.TabType)) return JsonResponseBuilder.BuildErrorJson("TabType is required.");

            try
            {
                var result = await ExecuteTenantOperation(orgId, async conn =>
                {
                    var sql = @"SELECT public.fn_amz_get_sales_inventory(
                            @p_page, @p_page_size, @p_sort_field, @p_sort_order,
                            @p_global_search, @p_tab_type, @p_filters);";

                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p_page", NpgsqlTypes.NpgsqlDbType.Integer, request.Page);
                    cmd.Parameters.AddWithValue("@p_page_size", NpgsqlTypes.NpgsqlDbType.Integer, request.PageSize);
                    cmd.Parameters.AddWithValue("@p_sort_field", NpgsqlTypes.NpgsqlDbType.Text, (object?)request.SortField ?? "asin");
                    cmd.Parameters.AddWithValue("@p_sort_order", NpgsqlTypes.NpgsqlDbType.Integer, request.SortOrder);
                    cmd.Parameters.AddWithValue("@p_global_search", NpgsqlTypes.NpgsqlDbType.Text, (object?)request.GlobalSearch ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_tab_type", NpgsqlTypes.NpgsqlDbType.Text, (object?)request.TabType ?? "product");
                    var filtersJson = request.Filters != null && request.Filters.Any() ? System.Text.Json.JsonSerializer.Serialize(request.Filters) : "{}";
                    cmd.Parameters.AddWithValue("@p_filters", NpgsqlTypes.NpgsqlDbType.Jsonb, filtersJson);

                    var jsonResult = await cmd.ExecuteScalarAsync();
                    if (jsonResult == null || jsonResult == DBNull.Value)
                        return JsonResponseBuilder.BuildErrorJson("No data returned from fn_get_inventory.");

                    return jsonResult.ToString() ?? JsonResponseBuilder.BuildErrorJson("Empty JSON response.");
                }, $"Get Inventory - {request.TabType}");

                return result.Success ? result.Data! : JsonResponseBuilder.BuildErrorJson(result.Message);
            }
            catch (PostgresException ex)
            {
                Logger.LogError(ex, "PostgreSQL error for OrgId: {OrgId}, TabType: {TabType}", orgId, request.TabType);
                return JsonResponseBuilder.BuildErrorJson($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error for OrgId: {OrgId}, TabType: {TabType}", orgId, request.TabType);
                return JsonResponseBuilder.BuildErrorJson($"Unexpected error: {ex.Message}");
            }
        }


    }
}
