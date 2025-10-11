using xQuantum_API.Models.Common;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Interfaces.Reports
{
    public interface IInventoryService
    {
        Task<ApiResponse<PaginatedResponse<Dictionary<string, object>>>> GetInventoryAsync(string OrgId, InventoryQueryRequest req);
    }
}
