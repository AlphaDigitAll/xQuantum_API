using Newtonsoft.Json;
using Npgsql;
using xQuantum_API.Interfaces;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.Reports;
using xQuantum_API.Repositories;

namespace xQuantum_API.Services.Reports
{
    public class InventoryService : IInventoryService
    {
        private readonly ILogger<InventoryService> _logger;
        private readonly ITenantService _tenantService;
        private readonly IConnectionStringManager _connectionManager;

        public InventoryService(ILogger<InventoryService> logger, ITenantService tenantService, IConnectionStringManager connectionManager)
        {
            _logger = logger;
            _tenantService = tenantService;
            _connectionManager = connectionManager;
        }
        public async Task<ApiResponse<PaginatedResponse<Dictionary<string, object>>>> GetInventoryAsync(string orgId, InventoryQueryRequest req)
        {
            var paginatedResponse = new PaginatedResponse<Dictionary<string, object>>
            {
                Page = req.Page,
                PageSize = req.PageSize
            };

            try
            {
                // ✅ Fetch cached connection string or load if missing
                var connectionString = await _connectionManager.GetOrAddConnectionStringAsync(orgId,
                    async () => await _tenantService.GetTenantConnectionStringAsync(orgId));

                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

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

                return ApiResponse<PaginatedResponse<Dictionary<string, object>>>.Ok(paginatedResponse, "Inventory fetched successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<Dictionary<string, object>>>.Fail($"Error fetching inventory: {ex.Message}");
            }
        }

    }
}
