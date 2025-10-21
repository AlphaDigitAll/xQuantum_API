using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using xQuantum_API.Interfaces.Products;
using xQuantum_API.Interfaces.Tenant;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.Products;
using xQuantum_API.Models.Reports;


namespace xQuantum_API.Services.Products
{
    public class SubProductColumnService : TenantAwareServiceBase, ISubProductColumnService
    {
        public SubProductColumnService(
            ILogger<SubProductColumnService> logger,
            ITenantService tenantService,
            IConnectionStringManager connectionManager)
            : base(connectionManager, tenantService, logger)
        {
        }

        public async Task<ApiResponse<int>> InsertAsync(string orgId, SubProductColumn model)
        {
            return await ExecuteTenantOperation(orgId, async conn =>
            {
                var sql = @"
                    INSERT INTO tbl_amz_sub_product_columns
                        (id, sub_id, column_name, profile_id, is_active, created_by, created_on)
                    VALUES
                        ((SELECT COALESCE(MAX(id),0)+1 FROM tbl_amz_sub_product_columns),
                         @sub_id, @column_name, @profile_id, true, @created_by, NOW())
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@sub_id", model.SubId);
                cmd.Parameters.AddWithValue("@column_name", model.ColumnName);
                cmd.Parameters.AddWithValue("@profile_id", model.ProfileId);
                cmd.Parameters.AddWithValue("@created_by", model.CreatedBy);

                return (int)await cmd.ExecuteScalarAsync();
            }, "Insert SubProductColumn");
        }

        public async Task<ApiResponse<bool>> UpdateAsync(string orgId, SubProductColumn model)
        {
            return await ExecuteTenantBoolOperation(orgId, async conn =>
            {
                var sql = @"
                    UPDATE tbl_amz_sub_product_columns
                    SET sub_id = @sub_id,
                        column_name = @column_name,
                        profile_id = @profile_id,
                        updated_by = @updated_by,
                        updated_on = NOW()
                    WHERE id = @id AND is_active = TRUE;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", model.Id);
                cmd.Parameters.AddWithValue("@sub_id", model.SubId);
                cmd.Parameters.AddWithValue("@column_name", model.ColumnName);
                cmd.Parameters.AddWithValue("@profile_id", model.ProfileId);
                cmd.Parameters.AddWithValue("@updated_by", model.UpdatedBy ?? Guid.Empty);

                return await cmd.ExecuteNonQueryAsync();
            }, "Update SubProductColumn");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string orgId, int id, Guid updatedBy)
        {
            return await ExecuteTenantBoolOperation(orgId, async conn =>
            {
                var sql = @"
                    UPDATE tbl_amz_sub_product_columns
                    SET is_active = FALSE,
                        updated_by = @updated_by,
                        updated_on = NOW()
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@updated_by", updatedBy);

                return await cmd.ExecuteNonQueryAsync();
            }, "Delete SubProductColumn");
        }

        public async Task<ApiResponse<List<SubProductColumn>>> GetBySubIdAsync(string orgId, Guid subId)
        {
            return await ExecuteTenantOperation(orgId, async conn =>
            {
                var sql = "SELECT * FROM tbl_amz_sub_product_columns WHERE sub_id = @sub_id AND is_active = TRUE";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@sub_id", subId);

                var result = new List<SubProductColumn>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new SubProductColumn
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        SubId = reader.GetGuid(reader.GetOrdinal("sub_id")),
                        ColumnName = reader.GetString(reader.GetOrdinal("column_name")),
                        ProfileId = reader.GetGuid(reader.GetOrdinal("profile_id")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                        CreatedBy = reader.GetGuid(reader.GetOrdinal("created_by")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("created_on")),
                        UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetGuid(reader.GetOrdinal("updated_by")),
                        UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_on")),
                    });
                }

                return result;
            }, "Get SubProductColumns By SubId");
        }

        public async Task<ApiResponse<List<SubProductColumn>>> GetByProfileIdAsync(string orgId, Guid profileId)
        {
            return await ExecuteTenantOperation(orgId, async conn =>
            {
                var sql = "SELECT * FROM tbl_amz_sub_product_columns WHERE profile_id = @profile_id AND is_active = TRUE";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@profile_id", profileId);

                var result = new List<SubProductColumn>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new SubProductColumn
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        SubId = reader.GetGuid(reader.GetOrdinal("sub_id")),
                        ColumnName = reader.GetString(reader.GetOrdinal("column_name")),
                        ProfileId = reader.GetGuid(reader.GetOrdinal("profile_id")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                        CreatedBy = reader.GetGuid(reader.GetOrdinal("created_by")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("created_on")),
                        UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetGuid(reader.GetOrdinal("updated_by")),
                        UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_on")),
                    });
                }

                return result;
            }, "Get SubProductColumns By ProfileId");
        }

        public async Task<ApiResponse<PaginatedResponseWithFooter<Dictionary<string, object>>>> GetProductsBySubIdAsync(string orgId, InventoryQueryRequest req)
        {
            try
            {
                return await ExecuteTenantOperation(orgId, async conn =>
                {
                    const string sql = "SELECT * FROM public.fn_amz_get_products_by_sub_id(@subId, @page, @pageSize, @sortField, @sortOrder, @globalSearch, @filters::jsonb)";

                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@subId", req.SubId);
                    cmd.Parameters.AddWithValue("@page", req.Page);
                    cmd.Parameters.AddWithValue("@pageSize", req.PageSize);
                    cmd.Parameters.AddWithValue("@sortField", string.IsNullOrWhiteSpace(req.SortField) ? "asin" : req.SortField);
                    cmd.Parameters.AddWithValue("@sortOrder", req.SortOrder == 0 ? 1 : req.SortOrder);
                    cmd.Parameters.AddWithValue("@globalSearch", (object?)req.GlobalSearch ?? DBNull.Value);

                    var filtersJson = JsonConvert.SerializeObject(req.Filters ?? new Dictionary<string, object>());
                    cmd.Parameters.AddWithValue("@filters", filtersJson);

                    await using var sdr = await cmd.ExecuteReaderAsync();

                    var paginatedResponse = new PaginatedResponseWithFooter<Dictionary<string, object>>
                    {
                        Page = req.Page,
                        PageSize = req.PageSize,
                        Records = new List<Dictionary<string, object>>(),
                        Footer = new Dictionary<string, object>()
                    };

                    long totalCount = 0;

                    while (await sdr.ReadAsync())
                    {
                        var record = new Dictionary<string, object>
                        {
                            ["product_asin"] = sdr.IsDBNull(sdr.GetOrdinal("product_asin")) ? string.Empty : sdr.GetString(sdr.GetOrdinal("product_asin")),
                            ["product_name"] = sdr.IsDBNull(sdr.GetOrdinal("product_name")) ? string.Empty : sdr.GetString(sdr.GetOrdinal("product_name")),
                            ["product_image"] = sdr.IsDBNull(sdr.GetOrdinal("product_image")) ? string.Empty : sdr.GetString(sdr.GetOrdinal("product_image"))
                        };

                        if (!sdr.IsDBNull(sdr.GetOrdinal("dynamic_data")))
                        {
                            var jsonData = sdr["dynamic_data"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(jsonData))
                            {
                                var dynamicFields = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                                if (dynamicFields != null)
                                {
                                    foreach (var field in dynamicFields)
                                        record[field.Key] = field.Value ?? string.Empty;
                                }
                            }
                        }

                        paginatedResponse.Records.Add(record);
                        totalCount++;
                    }

                    paginatedResponse.TotalRecords = totalCount;
                    return paginatedResponse;

                }, nameof(GetProductsBySubIdAsync));
            }
            catch (NpgsqlException ex)
            {
                return new ApiResponse<PaginatedResponseWithFooter<Dictionary<string, object>>>
                {
                    Success = false,
                    Message = $"Database error while fetching product data: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PaginatedResponseWithFooter<Dictionary<string, object>>>
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<int>> UpsertColumnValueAsync(string orgId, SubProductColumnValue model)
        {
            return await ExecuteTenantOperation(orgId, async conn =>
            {
                // First, check if a record exists with the same column_id, product_asin, and sub_id
                var checkSql = @"
                    SELECT id FROM tbl_amz_sub_product_column_values
                    WHERE column_id = @column_id
                      AND product_asin = @product_asin
                      AND sub_id = @sub_id
                    LIMIT 1;";

                await using var checkCmd = new NpgsqlCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@column_id", model.ColumnId);
                checkCmd.Parameters.AddWithValue("@product_asin", model.ProductAsin);
                checkCmd.Parameters.AddWithValue("@sub_id", model.SubId);

                var existingId = await checkCmd.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Record exists - UPDATE
                    var updateSql = @"
                        UPDATE tbl_amz_sub_product_column_values
                        SET value = @value,
                            updated_by = @updated_by,
                            updated_on = NOW()
                        WHERE id = @id
                        RETURNING id;";

                    await using var updateCmd = new NpgsqlCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@id", existingId);
                    updateCmd.Parameters.AddWithValue("@value", model.Value ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@updated_by", model.UpdatedBy ?? model.CreatedBy);

                    var result = await updateCmd.ExecuteScalarAsync();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
                else
                {
                    // Record doesn't exist - INSERT
                    var insertSql = @"
                        INSERT INTO tbl_amz_sub_product_column_values
                            (sub_id, product_asin, column_id, value, created_by, created_on)
                        VALUES
                            (@sub_id, @product_asin, @column_id, @value, @created_by, NOW())
                        RETURNING id;";

                    await using var insertCmd = new NpgsqlCommand(insertSql, conn);
                    insertCmd.Parameters.AddWithValue("@sub_id", model.SubId);
                    insertCmd.Parameters.AddWithValue("@product_asin", model.ProductAsin);
                    insertCmd.Parameters.AddWithValue("@column_id", model.ColumnId);
                    insertCmd.Parameters.AddWithValue("@value", model.Value ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@created_by", model.CreatedBy);

                    var result = await insertCmd.ExecuteScalarAsync();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }, "Upsert SubProductColumnValue");
        }

        /// <summary>
        /// ULTRA-FAST bulk upsert for product column values
        /// Uses PostgreSQL UNNEST + ON CONFLICT for maximum performance
        /// Can process 1000+ records in milliseconds
        /// </summary>
        public async Task<string> BulkUpsertColumnValuesAsync(string orgId, Guid subId, List<BulkUpsertColumnValueItem> items, Guid userId)
        {
            if (items == null || items.Count == 0)
            {
                return BuildBulkErrorJson("No items provided");
            }

            var response = await ExecuteTenantOperation(orgId, async conn =>
            {
                // Prepare arrays for PostgreSQL UNNEST
                var productAsins = items.Select(x => x.ProductAsin).ToArray();
                var columnIds = items.Select(x => x.ColumnId).ToArray();
                var values = items.Select(x => x.Value ?? string.Empty).ToArray();

                const string sql = @"
                    SELECT public.fn_bulk_upsert_product_column_values(
                        @p_sub_id,
                        @p_product_asins,
                        @p_column_ids,
                        @p_values,
                        @p_user_id
                    );";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p_sub_id", subId);
                cmd.Parameters.AddWithValue("@p_product_asins", productAsins);
                cmd.Parameters.AddWithValue("@p_column_ids", columnIds);
                cmd.Parameters.AddWithValue("@p_values", values);
                cmd.Parameters.AddWithValue("@p_user_id", userId);

                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? BuildBulkErrorJson("No data returned");

            }, $"Bulk Upsert {items.Count} Column Values");

            return response.Success ? response.Data ?? BuildBulkErrorJson("Empty data") : BuildBulkErrorJson(response.Message);
        }

        private string BuildBulkErrorJson(string message)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                message = message,
                data = new
                {
                    inserted = 0,
                    updated = 0,
                    total = 0
                }
            });
        }
    }
}