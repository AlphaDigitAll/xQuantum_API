using Newtonsoft.Json;
using Npgsql;
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
        public async Task<ApiResponse<PaginatedResponseWithFooter<Dictionary<string, object>>>> GetInventoryAsync(string orgId, InventoryQueryRequest req)
        {
            return await ExecuteTenantOperation(orgId, async conn =>
            {
                var paginatedResponse = new PaginatedResponseWithFooter<Dictionary<string, object>>
                {
                    Page = req.Page,
                    PageSize = req.PageSize
                };

                var sql = "SELECT * FROM public.fn_get_inventory(@page, @pageSize, @sortField, @sortOrder, @globalSearch, @filters::jsonb)";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@page", req.Page);
                cmd.Parameters.AddWithValue("@pageSize", req.PageSize);
                cmd.Parameters.AddWithValue("@sortField", string.IsNullOrWhiteSpace(req.SortField) ? "id" : req.SortField);
                cmd.Parameters.AddWithValue("@sortOrder", req.SortOrder == 0 ? 1 : req.SortOrder);
                cmd.Parameters.AddWithValue("@globalSearch", (object?)req.GlobalSearch ?? DBNull.Value);

                var filtersJson = JsonConvert.SerializeObject(req.Filters ?? new Dictionary<string, object>());
                cmd.Parameters.AddWithValue("@filters", filtersJson);

                await using var reader = await cmd.ExecuteReaderAsync();

                var items = new List<Dictionary<string, object>>();
                Dictionary<string, object> footer = null;
                long totalCount = 0;

                while (await reader.ReadAsync())
                {
                    var resultType = reader.GetString(reader.GetOrdinal("result_type"));

                    if (resultType == "record")
                    {
                        // This is a regular record
                        var dict = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);

                            // Skip result_type and NULL footer columns
                            if (columnName == "result_type" ||
                                (columnName == "working_quantity" && reader.IsDBNull(i)) ||
                                (columnName == "total_fulfillable_quantity" && reader.IsDBNull(i)) ||
                                (columnName == "shipped_quantity" && reader.IsDBNull(i)) ||
                                (columnName == "receiving_quantity" && reader.IsDBNull(i)) ||
                                (columnName == "customer_order_quantity" && reader.IsDBNull(i)) ||
                                (columnName == "transshipment_quantity" && reader.IsDBNull(i)) ||
                                (columnName == "fc_processing_quantity_total" && reader.IsDBNull(i)) ||
                                (columnName == "total_reserved_quantity_sum" && reader.IsDBNull(i)) ||
                                (columnName == "customer_damaged_quantity_total" && reader.IsDBNull(i)) ||
                                (columnName == "warehouse_damaged_quantity_total" && reader.IsDBNull(i)) ||
                                (columnName == "distributor_damaged_quantity_total" && reader.IsDBNull(i)) ||
                                (columnName == "carrier_damaged_quantity_total" && reader.IsDBNull(i)) ||
                                (columnName == "defective_quantity_total" && reader.IsDBNull(i)) ||
                                (columnName == "expired_quantity_total" && reader.IsDBNull(i)) ||
                                (columnName == "total_unfulfillable_quantity" && reader.IsDBNull(i)) ||
                                (columnName == "total_quantity_sum" && reader.IsDBNull(i)))
                            {
                                continue;
                            }

                            dict[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }

                        // Get total count
                        if (dict.ContainsKey("total_count") && dict["total_count"] != null)
                        {
                            totalCount = Convert.ToInt64(dict["total_count"]);
                        }

                        items.Add(dict);
                    }
                    else if (resultType == "footer")
                    {
                        // This is the footer row
                        footer = new Dictionary<string, object>
                {
                    { "sr_no", 0 },
                    { "image", null },
                    { "asin", null },
                    { "fnSku", null },
                    { "productName", null },
                    { "working_quantity", GetLongValue(reader, "working_quantity") },
                    { "total_fulfillable_quantity", GetLongValue(reader, "total_fulfillable_quantity") },
                    { "total_unfulfillable_quantity", GetLongValue(reader, "total_unfulfillable_quantity") },
                    { "shipped_quantity", GetLongValue(reader, "shipped_quantity") },
                    { "receiving_quantity", GetLongValue(reader, "receiving_quantity") },
                    { "customer_order_quantity", GetLongValue(reader, "customer_order_quantity") },
                    { "transshipment_quantity", GetLongValue(reader, "transshipment_quantity") },
                    { "fc_processing_quantity", GetLongValue(reader, "fc_processing_quantity_total") },
                    { "total_reserved_quantity", GetLongValue(reader, "total_reserved_quantity_sum") },
                    { "customer_damaged_quantity", GetLongValue(reader, "customer_damaged_quantity_total") },
                    { "warehouse_damaged_quantity", GetLongValue(reader, "warehouse_damaged_quantity_total") },
                    { "distributor_damaged_quantity", GetLongValue(reader, "distributor_damaged_quantity_total") },
                    { "carrier_damaged_quantity", GetLongValue(reader, "carrier_damaged_quantity_total") },
                    { "defective_quantity", GetLongValue(reader, "defective_quantity_total") },
                    { "expired_quantity", GetLongValue(reader, "expired_quantity_total") },
                    { "total_quantity", GetLongValue(reader, "total_quantity_sum") }
                };
                    }
                }

                paginatedResponse.Records = items;
                paginatedResponse.TotalRecords = totalCount;
                paginatedResponse.Footer = footer ?? new Dictionary<string, object>();

                return paginatedResponse;
            }, "Get Inventory");
        }
        private long GetLongValue(NpgsqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt64(ordinal);
        }


    }
}
