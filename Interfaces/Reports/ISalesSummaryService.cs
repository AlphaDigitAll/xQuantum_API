using xQuantum_API.Models.Reports;

namespace xQuantum_API.Interfaces.Reports
{
    /// <summary>
    /// Service interface for optimized sales summary operations
    /// Returns JSON directly from database for maximum performance
    /// </summary>
    public interface ISalesSummaryService
    {
        public Task<string> GetSalesSummaryCardsJsonAsync(string orgId, SummaryCardRequest request);
        /// <summary>
        /// Retrieves sales summary data as raw JSON from PostgreSQL
        /// Zero C# conversion overhead - database does all formatting
        /// </summary>
        /// <param name="orgId">Organization ID for tenant isolation</param>
        /// <param name="request">Filter and pagination parameters</param>
        /// <returns>Raw JSON string with sales summary data</returns>
        public Task<string> GetSellerSalesSummaryJsonAsync(string orgId, SummaryFilterRequest request);

        /// <summary>
        /// Retrieves sales heatmap data as raw JSON from PostgreSQL
        /// Returns 7x24 array (days of week x hours) with aggregated metrics
        /// Zero C# conversion overhead - database does all formatting
        /// </summary>
        /// <param name="orgId">Organization ID for tenant isolation</param>
        /// <param name="request">Heatmap request with tab type, sub_id, and date range</param>
        /// <returns>Raw JSON string with heatmap data</returns>
        public Task<string> GetSalesHeatmapJsonAsync(string orgId, SalesHeatmapRequest request);
    }
}
