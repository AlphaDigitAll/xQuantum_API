using xQuantum_API.Models.Common;
using xQuantum_API.Models.Products;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Interfaces.Products
{
    public interface ISubProductColumnService
    {
        Task<ApiResponse<int>> InsertAsync(string orgId, SubProductColumn model);
        Task<ApiResponse<bool>> UpdateAsync(string orgId, SubProductColumn model);
        Task<ApiResponse<List<SubProductColumn>>> GetBySubIdAsync(string orgId, Guid subId);
        Task<ApiResponse<List<SubProductColumn>>> GetByProfileIdAsync(string orgId, Guid profileId);
        Task<ApiResponse<PaginatedResponseWithFooter<Dictionary<string, object>>>> GetProductsBySubIdAsync(string orgId, InventoryQueryRequest req);
        Task<ApiResponse<bool>> DeleteAsync(string orgId, int id, Guid updatedBy);
        Task<ApiResponse<int>> UpsertColumnValueAsync(string orgId, SubProductColumnValue model);
    }
}
