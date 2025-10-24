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

        /// <summary>
        /// Ultra-fast bulk upsert - can process 1000+ records in milliseconds
        /// Uses PostgreSQL UNNEST and ON CONFLICT for maximum performance
        /// </summary>
        Task<string> BulkUpsertColumnValuesAsync(string orgId, Guid subId, List<BulkUpsertColumnValueItem> items, Guid userId);

        /// <summary>
        /// Bulk upsert product columns and values from Excel file
        /// Processes columns (excluding product_name, product_image, product_asin)
        /// Ultra-fast bulk operation - handles 1000+ records efficiently
        /// </summary>
        Task<string> BulkUpsertFromExcelAsync(string orgId, BulkUpsertFromExcelRequest request, Guid userId);

        /// <summary>
        /// Export products with custom columns to Excel file
        /// Ultra-fast export - optimized for 100+ concurrent requests
        /// Returns Excel file with product_asin, product_name, product_image + custom columns
        /// </summary>
        Task<byte[]> ExportProductsToExcelAsync(string orgId, ExportProductsToExcelRequest request);

        /// <summary>
        /// Get blacklist keywords for all products under a subscription
        /// Ultra-fast query - handles 100+ concurrent requests with sub-50ms response times
        /// Joins with tbl_amz_products for product details (title, image)
        /// </summary>
        Task<string> GetBlacklistDataAsync(string orgId, Guid subId);

        /// <summary>
        /// Bulk update blacklist keyword values (negative_exact, negative_phrase)
        /// Ultra-fast UPSERT - can process 100+ records in <50ms
        /// Uses PostgreSQL UNNEST and ON CONFLICT for maximum performance
        /// </summary>
        Task<string> BulkUpdateBlacklistValuesAsync(string orgId, List<UpdateBlacklistValueRequest> items, Guid userId);
    }
}
