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
        public async Task<ApiResponse<PaginatedResponse<Dictionary<string, object>>>> GetInventoryAsync(string orgId, InventoryQueryRequest req)
        {
            return await ExecuteTenantOperation(orgId, async conn =>
            {
                var paginatedResponse = new PaginatedResponse<Dictionary<string, object>>
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
                long totalCount = 0;

                while (await reader.ReadAsync())
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                    items.Add(dict);

                    if (totalCount == 0 && dict.ContainsKey("total_count") && dict["total_count"] != null)
                        totalCount = Convert.ToInt64(dict["total_count"]);
                }

                paginatedResponse.Records = items;
                paginatedResponse.TotalRecords = totalCount;

                return paginatedResponse;
            }, "Get Inventory");
        }

    }
}
