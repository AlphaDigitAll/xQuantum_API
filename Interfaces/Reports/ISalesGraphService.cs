using xQuantum_API.Models.Reports;

namespace xQuantum_API.Interfaces.Reports
{
    /// <summary>
    /// Service interface for sales graph aggregate data
    /// Returns JSON directly from database for maximum performance
    /// </summary>
    public interface ISalesGraphService
    {
        /// <summary>
        /// Retrieves aggregated sales graph data as raw JSON from PostgreSQL
        /// Returns total_sales, total_units, and total_aov
        /// Zero C# conversion overhead - database does all formatting
        /// </summary>
        /// <param name="orgId">Organization ID for tenant isolation</param>
        /// <param name="request">Filter parameters (no pagination)</param>
        /// <returns>Raw JSON string with aggregated graph data</returns>
        Task<string> GetSalesGraphAggregatesJsonAsync(string orgId, GraphFilterRequest request);
    }
}
