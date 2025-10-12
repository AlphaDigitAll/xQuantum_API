using xQuantum_API.Models.Common;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Interfaces.Reports
{
    public interface IInventoryService
    {
        Task<ApiResponse<PaginatedResponseWithFooter<Dictionary<string, object>>>> GetInventoryAsync(string orgId, InventoryQueryRequest req);    }
}
