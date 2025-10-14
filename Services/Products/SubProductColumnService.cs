using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using xQuantum_API.Interfaces.Products;
using xQuantum_API.Interfaces.Tenant;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.Products;


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

        public async Task<ApiResponse<List<ProductDetail>>> GetProductsBySubIdAsync(string orgId, Guid subId)
        {
            try
            {
                return await ExecuteTenantOperation(orgId, async conn =>
                {
                    const string sql = "SELECT * FROM public.fn_amz_get_products_by_sub_id(@subId)";

                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@subId", subId);

                    await using var sdr = await cmd.ExecuteReaderAsync();

                    var products = new List<ProductDetail>();

                    while (await sdr.ReadAsync())
                    {
                        products.Add(new ProductDetail
                        {
                            product_asin = sdr.IsDBNull(sdr.GetOrdinal("product_asin")) ? string.Empty : sdr.GetString(sdr.GetOrdinal("product_asin")),
                            product_name = sdr.IsDBNull(sdr.GetOrdinal("product_name")) ? string.Empty : sdr.GetString(sdr.GetOrdinal("product_name")),
                            product_image = sdr.IsDBNull(sdr.GetOrdinal("product_image")) ? string.Empty : sdr.GetString(sdr.GetOrdinal("product_image")),
                            dynamic_fields = !sdr.IsDBNull(sdr.GetOrdinal("dynamic_data")) && !string.IsNullOrWhiteSpace(sdr["dynamic_data"]?.ToString())
                                 ? JsonConvert.DeserializeObject<Dictionary<string, object>>(sdr["dynamic_data"].ToString()!)?.ToDictionary(k => k.Key, v => v.Value ?? string.Empty) ?? new Dictionary<string, object>()
                                 : new Dictionary<string, object>()
                        });
                    }


                    return products;

                }, "Get Products By SubId");
            }
            catch (NpgsqlException npgEx)
            {
                return new ApiResponse<List<ProductDetail>>
                {
                    Success = false,
                    Message = $"Database error while fetching product data: {npgEx.Message}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<ProductDetail>>
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}