using xQuantum_API.Models.Common;
using xQuantum_API.Models.Products;

namespace xQuantum_API.Interfaces.Products
{
    public interface ISubProductColumnService
    {
        Task<ApiResponse<int>> InsertAsync(string orgId, SubProductColumn model);
        Task<ApiResponse<bool>> UpdateAsync(string orgId, SubProductColumn model);
        Task<ApiResponse<List<SubProductColumn>>> GetBySubIdAsync(string orgId, Guid subId);
        Task<ApiResponse<List<SubProductColumn>>> GetByProfileIdAsync(string orgId, Guid profileId);
        Task<ApiResponse<bool>> DeleteAsync(string orgId, int id, Guid updatedBy);

    }
}
