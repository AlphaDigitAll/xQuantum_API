using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using OfficeOpenXml;
using xQuantum_API.Helpers;
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
                // UPSERT: Insert or update if duplicate column name exists
                var sql = @"
                    INSERT INTO tbl_amz_sub_product_columns
                        (id, sub_id, column_name, profile_id, is_active, created_by, created_on)
                    VALUES
                        ((SELECT COALESCE(MAX(id),0)+1 FROM tbl_amz_sub_product_columns),
                         @sub_id, @column_name, @profile_id, true, @created_by, NOW())
                    ON CONFLICT (sub_id, column_name, profile_id)
                    DO UPDATE SET
                        is_active = true,
                        updated_by = @created_by,
                        updated_on = NOW()
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@sub_id", model.SubId);
                cmd.Parameters.AddWithValue("@column_name", model.ColumnName);
                cmd.Parameters.AddWithValue("@profile_id", model.ProfileId);
                cmd.Parameters.AddWithValue("@created_by", model.CreatedBy);

                return (int)await cmd.ExecuteScalarAsync();
            }, "Upsert SubProductColumn");
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
                return JsonResponseBuilder.BuildBulkErrorJson("No items provided");
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
                return result?.ToString() ?? JsonResponseBuilder.BuildBulkErrorJson("No data returned");

            }, $"Bulk Upsert {items.Count} Column Values");

            return response.Success ? response.Data ?? JsonResponseBuilder.BuildBulkErrorJson("Empty data") : JsonResponseBuilder.BuildBulkErrorJson(response.Message);
        }


        public async Task<string> BulkUpsertFromExcelAsync(string orgId, BulkUpsertFromExcelRequest request, Guid userId)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Set EPPlus license context (required for non-commercial use)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // Read Excel file
                using var stream = request.ExcelFile.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0]; // Get first worksheet

                if (worksheet.Dimension == null)
                {
                    return JsonResponseBuilder.BuildBulkErrorJson("Excel file is empty");
                }

                var rowCount = worksheet.Dimension.End.Row;
                var colCount = worksheet.Dimension.End.Column;

                if (rowCount < 2)
                {
                    return JsonResponseBuilder.BuildBulkErrorJson("Excel must have at least a header row and one data row");
                }

                // ========================================
                // STEP 1: Extract column names from header (row 1)
                // ========================================
                var excludedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "product_name",
                    "product_image",
                    "product_asin"
                };

                var columnNames = new List<string>();
                var columnIndexes = new List<int>(); // Track which Excel columns to process
                int asinColumnIndex = -1;

                for (int col = 1; col <= colCount; col++)
                {
                    var headerValue = worksheet.Cells[1, col].Text?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(headerValue))
                        continue;

                    if (headerValue.Equals("product_asin", StringComparison.OrdinalIgnoreCase))
                    {
                        asinColumnIndex = col;
                    }
                    else if (!excludedColumns.Contains(headerValue))
                    {
                        columnNames.Add(headerValue);
                        columnIndexes.Add(col);
                    }
                }

                if (asinColumnIndex == -1)
                {
                    return JsonResponseBuilder.BuildBulkErrorJson("Excel must have a 'product_asin' column");
                }

                if (columnNames.Count == 0)
                {
                    return JsonResponseBuilder.BuildBulkErrorJson("No valid columns found (all columns are excluded)");
                }

                // ========================================
                // STEP 2: Extract product ASINs and values
                // ========================================
                var productAsins = new List<string>();
                var columnValuesMatrix = new List<List<string>>(); // 2D list

                for (int row = 2; row <= rowCount; row++)
                {
                    var asin = worksheet.Cells[row, asinColumnIndex].Text?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(asin))
                        continue; // Skip rows without ASIN

                    productAsins.Add(asin);

                    var rowValues = new List<string>();
                    foreach (var colIndex in columnIndexes)
                    {
                        var cellValue = worksheet.Cells[row, colIndex].Text?.Trim() ?? string.Empty;
                        rowValues.Add(cellValue);
                    }

                    columnValuesMatrix.Add(rowValues);
                }

                if (productAsins.Count == 0)
                {
                    return JsonResponseBuilder.BuildBulkErrorJson("No valid product ASINs found in Excel");
                }

                // ========================================
                // STEP 3: Flatten data for PostgreSQL
                // ========================================
                // Convert 2D matrix to flattened 1D arrays
                var flatProductAsins = new List<string>();
                var flatColumnNames = new List<string>();
                var flatValues = new List<string>();

                for (int i = 0; i < productAsins.Count; i++)
                {
                    for (int j = 0; j < columnNames.Count; j++)
                    {
                        flatProductAsins.Add(productAsins[i]);
                        flatColumnNames.Add(columnNames[j]);
                        flatValues.Add(columnValuesMatrix[i][j]);
                    }
                }

                // ========================================
                // STEP 4: Call PostgreSQL function
                // ========================================
                var response = await ExecuteTenantOperation(orgId, async conn =>
                {
                    const string sql = @"
                        SELECT fn_bulk_upsert_product_columns_from_excel(
                            @p_sub_id,
                            @p_profile_id,
                            @p_created_by,
                            @p_column_names,
                            @p_product_asins_flat,
                            @p_column_names_flat,
                            @p_values,
                            @p_org_id
                        );";

                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p_sub_id", request.SubId);
                    cmd.Parameters.AddWithValue("@p_profile_id", request.ProfileId);
                    cmd.Parameters.AddWithValue("@p_created_by", userId);
                    cmd.Parameters.AddWithValue("@p_column_names", columnNames.ToArray());
                    cmd.Parameters.AddWithValue("@p_product_asins_flat", flatProductAsins.ToArray());
                    cmd.Parameters.AddWithValue("@p_column_names_flat", flatColumnNames.ToArray());
                    cmd.Parameters.AddWithValue("@p_values", flatValues.ToArray());
                    cmd.Parameters.AddWithValue("@p_org_id", orgId ?? string.Empty);

                    var result = await cmd.ExecuteScalarAsync();
                    return result?.ToString() ?? JsonResponseBuilder.BuildBulkErrorJson("No data returned from database");

                }, $"Bulk Upsert from Excel: {productAsins.Count} products, {columnNames.Count} columns");

                return response.Success
                    ? response.Data ?? JsonResponseBuilder.BuildBulkErrorJson("Empty data")
                    : JsonResponseBuilder.BuildBulkErrorJson(response.Message);
            }
            catch (Exception ex)
            {
                return JsonResponseBuilder.BuildBulkErrorJson($"Exception: {ex.Message}");
            }
        }


        public async Task<byte[]> ExportProductsToExcelAsync(string orgId, ExportProductsToExcelRequest request)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var response = await ExecuteTenantOperation(orgId, async conn =>
            {
                const string sql = @"
            SELECT fn_export_products_with_columns(
                @p_sub_id,
                @p_profile_id,
                @p_org_id
            );";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p_sub_id", request.SubId);
                cmd.Parameters.AddWithValue("@p_profile_id", request.ProfileId.HasValue ? request.ProfileId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@p_org_id", orgId ?? string.Empty);

                var result = await cmd.ExecuteScalarAsync();
                var jsonResult = result?.ToString() ?? string.Empty;

                var jsonDoc = JObject.Parse(jsonResult);
                if (!(jsonDoc["success"]?.Value<bool>() ?? false))
                    throw new Exception(jsonDoc["message"]?.Value<string>() ?? "Export failed");

                var dataArray = jsonDoc["data"] as JArray;
                if (dataArray == null || dataArray.Count == 0)
                    throw new Exception("No products found to export.");

                var firstObj = (JObject)dataArray.First;
                var allColumns = firstObj.Properties().Select(p => p.Name).ToList();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Products");

                for (int i = 0; i < allColumns.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = allColumns[i];
                }

                using (var headerRange = worksheet.Cells[1, 1, 1, allColumns.Count])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);
                    headerRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var product in dataArray)
                {
                    for (int col = 0; col < allColumns.Count; col++)
                    {
                        worksheet.Cells[row, col + 1].Value = product[allColumns[col]]?.ToString() ?? string.Empty;
                    }
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                worksheet.View.FreezePanes(2, 1);

                return package.GetAsByteArray();

            }, $"Export products to Excel: SubId={request.SubId}");

            if (!response.Success)
                throw new Exception(response.Message);

            return response.Data ?? Array.Empty<byte>();
        }

        /// <summary>
        /// Get blacklist keywords for all products under a subscription
        /// Ultra-fast query using PostgreSQL function - handles 100+ concurrent requests
        /// Returns JSON with product details (title, image) + blacklist keywords
        /// </summary>
        public async Task<string> GetBlacklistDataAsync(string orgId, Guid subId)
        {
            try
            {
                var result = await ExecuteTenantOperation(orgId, async conn =>
                {
                    const string sql = "SELECT fn_get_blacklist_data(@sub_id);";
                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@sub_id", subId);

                    var res = await cmd.ExecuteScalarAsync();
                    return res?.ToString() ?? JsonResponseBuilder.BuildErrorJson("No data returned");
                },
                $"Get Blacklist Data - SubId: {subId}");

                return result.Success
                    ? result.Data ?? JsonResponseBuilder.BuildErrorJson("Empty response")
                    : JsonResponseBuilder.BuildErrorJson(result.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetBlacklistDataAsync failed for SubId: {SubId}", subId);
                return JsonResponseBuilder.BuildErrorJson($"Database error: {ex.Message}");
            }
        }

        /// <summary>
        /// Bulk update blacklist keyword values (negative_exact, negative_phrase)
        /// Ultra-fast UPSERT using PostgreSQL UNNEST - processes 100+ records in <50ms
        /// Supports dynamic column updates: "negative_exact" or "negative_phrase"
        /// </summary>
        public async Task<string> BulkUpdateBlacklistValuesAsync(string orgId, List<UpdateBlacklistValueRequest> items, Guid userId)
        {
            try
            {
                if (items == null || items.Count == 0)
                    return JsonResponseBuilder.BuildErrorJson("No items provided.");

                if (items.Count > 10000)
                    return JsonResponseBuilder.BuildErrorJson("Maximum 10,000 items allowed per request.");

                var validKeys = new HashSet<string> { "negative_exact", "negative_phrase" };
                var invalidItems = items
                    .Where(i => string.IsNullOrWhiteSpace(i.ColumnKey) || !validKeys.Contains(i.ColumnKey.ToLower()))
                    .ToList();

                if (invalidItems.Any())
                {
                    return JsonResponseBuilder.BuildErrorJson(
                        $"Invalid column_key values. Must be 'negative_exact' or 'negative_phrase'. Found: {string.Join(", ", invalidItems.Select(i => i.ColumnKey).Distinct())}"
                    );
                }

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
                        MAX(CASE WHEN column_key = 'negative_exact' THEN column_value END) AS negative_exact,
                        MAX(CASE WHEN column_key = 'negative_phrase' THEN column_value END) AS negative_phrase
                    FROM updates
                    GROUP BY sub_id, asin
                ),
                upserted AS (
                    INSERT INTO tbl_amz_product_blacklist_keywords
                        (sub_id, asin, negative_exact, negative_phrase, is_active, created_by, created_on, updated_by, updated_on)
                    SELECT
                        a.sub_id,
                        a.asin,
                        COALESCE(a.negative_exact, ''),
                        COALESCE(a.negative_phrase, ''),
                        TRUE,
                        @user_id,
                        NOW(),
                        @user_id,
                        NOW()
                    FROM aggregated a
                    ON CONFLICT (sub_id, asin)
                    DO UPDATE SET
                        negative_exact = CASE
                            WHEN EXCLUDED.negative_exact IS NOT NULL AND EXCLUDED.negative_exact <> ''
                            THEN EXCLUDED.negative_exact
                            ELSE tbl_amz_product_blacklist_keywords.negative_exact
                        END,
                        negative_phrase = CASE
                            WHEN EXCLUDED.negative_phrase IS NOT NULL AND EXCLUDED.negative_phrase <> ''
                            THEN EXCLUDED.negative_phrase
                            ELSE tbl_amz_product_blacklist_keywords.negative_phrase
                        END,
                        updated_by = @user_id,
                        updated_on = NOW(),
                        is_active = TRUE
                    RETURNING
                        CASE WHEN xmax = 0 THEN 1 ELSE 0 END AS inserted,
                        CASE WHEN xmax > 0 THEN 1 ELSE 0 END AS updated
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

                    var dbResult = await cmd.ExecuteScalarAsync();
                    return dbResult?.ToString() ?? JsonResponseBuilder.BuildErrorJson("No data returned.");
                },
                $"Bulk Update Blacklist Values - {items.Count} items");

                return result.Success
                    ? result.Data ?? JsonResponseBuilder.BuildErrorJson("Empty response from database.")
                    : JsonResponseBuilder.BuildErrorJson(result.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "BulkUpdateBlacklistValuesAsync failed for {Count} items", items?.Count ?? 0);
                return JsonResponseBuilder.BuildErrorJson($"Unexpected error: {ex.Message}");
            }
        }

    }
}