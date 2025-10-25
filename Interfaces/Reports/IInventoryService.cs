using xQuantum_API.Models.Common;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Interfaces.Reports
{
    public interface IInventoryService
    {
        public Task<string> GetInventoryJsonAsync(string orgId, InventoryQueryRequest request);
        public Task<string> GetInventorySalesJsonAsync(string orgId, InventoryQueryRequest request);
        Task<ApiResponse<InventoryCardSummary>> GetInventoryCardAsync(string connectionString, Guid subId);

    }

}
