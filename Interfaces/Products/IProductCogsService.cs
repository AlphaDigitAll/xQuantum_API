using xQuantum_API.Models.Products;

namespace xQuantum_API.Interfaces.Products
{
    /// <summary>
    /// Service interface for product COGS (Cost of Goods Sold) operations
    /// </summary>
    public interface IProductCogsService
    {
        /// <summary>
        /// Retrieves product COGS data with pagination, sorting, and search
        /// Returns raw JSON string from database (zero C# conversion overhead)
        /// </summary>
        /// <param name="orgId">Organization ID</param>
        /// <param name="request">Product COGS request parameters</param>
        /// <returns>Raw JSON string containing paginated COGS data</returns>
        Task<string> GetProductCogsDataJsonAsync(string orgId, ProductCogsRequest request);

        /// <summary>
        /// Bulk update product COGS values (fob, end_to_end, import_duty)
        /// Ultra-fast UPSERT - can process 100+ records in <50ms
        /// Returns raw JSON string with operation statistics
        /// </summary>
        /// <param name="orgId">Organization ID</param>
        /// <param name="items">List of COGS updates</param>
        /// <param name="userId">User performing the update</param>
        /// <returns>Raw JSON string with inserted/updated counts</returns>
        Task<string> BulkUpdateProductCogsAsync(string orgId, List<UpdateProductCogsRequest> items, Guid userId);
    }
}
