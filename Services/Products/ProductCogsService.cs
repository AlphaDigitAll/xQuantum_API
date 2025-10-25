using Npgsql;
using NpgsqlTypes;
using xQuantum_API.Helpers;
using xQuantum_API.Interfaces.Products;
using xQuantum_API.Interfaces.Tenant;
using xQuantum_API.Models.Products;

namespace xQuantum_API.Services.Products
{
    /// <summary>
    /// ULTRA-OPTIMIZED Service for product COGS data
    /// Returns JSON directly from PostgreSQL with ZERO C# conversion overhead
    /// Database does all the heavy lifting - C# just passes it through
    /// </summary>
    public class ProductCogsService : TenantAwareServiceBase, IProductCogsService
    {
        public ProductCogsService(
            ILogger<ProductCogsService> logger,
            ITenantService tenantService,
            IConnectionStringManager connectionManager)
            : base(connectionManager, tenantService, logger)
        {
        }

        public async Task<string> GetProductCogsDataJsonAsync(string orgId, ProductCogsRequest request)
        {
            // Validate required fields
            if (request == null)
                return JsonResponseBuilder.BuildErrorJson("Request is required.");

            if (request.SubId == Guid.Empty)
                return JsonResponseBuilder.BuildErrorJson("SubId is required.");

            // Normalize and validate input parameters
            request.Page = Math.Max(request.Page, 1);
            request.PageSize = Math.Max(1, Math.Min(request.PageSize, 1000));
            request.SortField = string.IsNullOrWhiteSpace(request.SortField) ? "asin" : request.SortField.ToLower();
            request.GlobalSearch = request.GlobalSearch?.Trim() ?? string.Empty;

            try
            {
                var result = await ExecuteTenantOperation(orgId, async conn =>
                {
                    var sql = @"
                        SELECT fn_get_product_cogs_v2(
                            @p_sub_id,
                            @p_page,
                            @p_page_size,
                            @p_sort_field,
                            @p_sort_order,
                            @p_global_search
                        );";

                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p_sub_id", NpgsqlDbType.Uuid, request.SubId);
                    cmd.Parameters.AddWithValue("@p_page", NpgsqlDbType.Integer, request.Page);
                    cmd.Parameters.AddWithValue("@p_page_size", NpgsqlDbType.Integer, request.PageSize);
                    cmd.Parameters.AddWithValue("@p_sort_field", NpgsqlDbType.Text, request.SortField);
                    cmd.Parameters.AddWithValue("@p_sort_order", NpgsqlDbType.Integer, request.SortOrder);
                    cmd.Parameters.AddWithValue("@p_global_search", NpgsqlDbType.Text, request.GlobalSearch);

                    Logger.LogInformation(
                        "Fetching product COGS data for SubId: {SubId}, Page: {Page}, PageSize: {PageSize}, SortField: {SortField}, Search: '{Search}'",
                        request.SubId, request.Page, request.PageSize, request.SortField, request.GlobalSearch);

                    var jsonResult = await cmd.ExecuteScalarAsync();

                    if (jsonResult == null || jsonResult == DBNull.Value)
                        return JsonResponseBuilder.BuildErrorJson("No data returned from database.");

                    return jsonResult.ToString() ?? JsonResponseBuilder.BuildErrorJson("Failed to retrieve data.");
                }, $"Get Product COGS Data - SubId: {request.SubId}");

                return result.Success ? result.Data! : JsonResponseBuilder.BuildErrorJson(result.Message);
            }
            catch (PostgresException pgEx)
            {
                Logger.LogError(pgEx,
                    "PostgreSQL error while fetching product COGS data for SubId: {SubId}",
                    request.SubId);
                return JsonResponseBuilder.BuildErrorJson($"Database error: {pgEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Unexpected error while fetching product COGS data for SubId: {SubId}",
                    request.SubId);
                return JsonResponseBuilder.BuildErrorJson($"Unexpected error: {ex.Message}");
            }
        }

        public async Task<string> BulkUpdateProductCogsAsync(string orgId, List<UpdateProductCogsRequest> items, Guid userId)
        {
            try
            {
                if (items == null || items.Count == 0)
                    return JsonResponseBuilder.BuildErrorJson("No items provided.");

                if (items.Count > 10000)
                    return JsonResponseBuilder.BuildErrorJson("Maximum 10,000 items allowed per request.");

                var result = await ExecuteTenantOperation(orgId, async conn =>
                {
                    const string sql = @"
                WITH updates AS (
                    SELECT
                        u.sub_id,
                        u.asin,
                        u.column_key,
                        u.column_value
                    FROM UNNEST(
                        @sub_ids::uuid[],
                        @asins::varchar[],
                        @column_keys::varchar[],
                        @column_values::text[]
                    ) AS u(sub_id, asin, column_key, column_value)
                ),
                aggregated AS (
                    SELECT
                        sub_id,
                        asin,
                        MAX(CASE WHEN column_key = 'fob' THEN column_value END) AS fob,
                        MAX(CASE WHEN column_key = 'end_to_end' THEN column_value END) AS end_to_end,
                        MAX(CASE WHEN column_key = 'import_duty' THEN column_value END) AS import_duty
                    FROM updates
                    GROUP BY sub_id, asin
                ),
                upserted AS (
                    INSERT INTO tbl_amz_product_cogs
                        (sub_id, asin, fob, end_to_end, import_duty, is_active, created_by, created_on, updated_by, updated_on)
                    SELECT
                        a.sub_id,
                        a.asin,
                        COALESCE(a.fob, ''),
                        COALESCE(a.end_to_end, ''),
                        COALESCE(a.import_duty, ''),
                        TRUE,
                        @user_id,
                        NOW(),
                        @user_id,
                        NOW()
                    FROM aggregated a
                    ON CONFLICT (sub_id, asin)
                    DO UPDATE SET
                        fob = CASE
                            WHEN EXCLUDED.fob IS NOT NULL AND EXCLUDED.fob <> ''
                            THEN EXCLUDED.fob
                            ELSE tbl_amz_product_cogs.fob
                        END,
                        end_to_end = CASE
                            WHEN EXCLUDED.end_to_end IS NOT NULL AND EXCLUDED.end_to_end <> ''
                            THEN EXCLUDED.end_to_end
                            ELSE tbl_amz_product_cogs.end_to_end
                        END,
                        import_duty = CASE
                            WHEN EXCLUDED.import_duty IS NOT NULL AND EXCLUDED.import_duty <> ''
                            THEN EXCLUDED.import_duty
                            ELSE tbl_amz_product_cogs.import_duty
                        END,
                        updated_by = @user_id,
                        updated_on = NOW(),
                        is_active = TRUE
                    RETURNING
                        CASE WHEN xmax::text::bigint = 0 THEN 1 ELSE 0 END AS inserted,
                        CASE WHEN xmax::text::bigint > 0 THEN 1 ELSE 0 END AS updated
                )
                SELECT jsonb_build_object(
                    'success', TRUE,
                    'message', 'Processed ' || COUNT(*) || ' records: ' || SUM(inserted) || ' inserted, ' || SUM(updated) || ' updated',
                    'data', jsonb_build_object(
                        'inserted', SUM(inserted),
                        'updated', SUM(updated),
                        'total', COUNT(*)
                    )
                )
                FROM upserted;";

                    await using var cmd = new NpgsqlCommand(sql, conn);

                    var subIds = items.Select(i => i.SubId).ToArray();
                    var asins = items.Select(i => i.Asin).ToArray();
                    var columnKeys = items.Select(i => i.ColumnKey.ToLower()).ToArray();
                    var columnValues = items.Select(i => i.ColumnValue ?? string.Empty).ToArray();

                    cmd.Parameters.AddWithValue("@sub_ids", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Uuid, subIds);
                    cmd.Parameters.AddWithValue("@asins", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Varchar, asins);
                    cmd.Parameters.AddWithValue("@column_keys", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Varchar, columnKeys);
                    cmd.Parameters.AddWithValue("@column_values", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text, columnValues);
                    cmd.Parameters.AddWithValue("@user_id", NpgsqlTypes.NpgsqlDbType.Uuid, userId);

                    Logger.LogInformation(
                        "Bulk updating product COGS for {Count} items",
                        items.Count);

                    var dbResult = await cmd.ExecuteScalarAsync();
                    return dbResult?.ToString() ?? JsonResponseBuilder.BuildErrorJson("No data returned.");
                },
                $"Bulk Update Product COGS - {items.Count} items");

                return result.Success
                    ? result.Data ?? JsonResponseBuilder.BuildErrorJson("Empty response from database.")
                    : JsonResponseBuilder.BuildErrorJson(result.Message);
            }
            catch (PostgresException pgEx)
            {
                Logger.LogError(pgEx, "PostgreSQL error in BulkUpdateProductCogsAsync for {Count} items", items?.Count ?? 0);
                return JsonResponseBuilder.BuildErrorJson($"Database error: {pgEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "BulkUpdateProductCogsAsync failed for {Count} items", items?.Count ?? 0);
                return JsonResponseBuilder.BuildErrorJson($"Unexpected error: {ex.Message}");
            }
        }
    }
}
