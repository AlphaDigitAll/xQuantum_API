using xQuantum_API.Models.Reports;

namespace xQuantum_API.Interfaces.Reports
{
    /// <summary>
    /// Service interface for optimized sales summary operations
    /// Returns JSON directly from database for maximum performance
    /// </summary>
    public interface ISalesSummaryService
    {
        /// <summary>
        /// Retrieves sales summary data as raw JSON from PostgreSQL
        /// Zero C# conversion overhead - database does all formatting
        /// </summary>
        /// <param name="orgId">Organization ID for tenant isolation</param>
        /// <param name="request">Filter and pagination parameters</param>
        /// <returns>Raw JSON string with sales summary data</returns>
        Task<string> GetSalesSummaryJsonAsync(string orgId, SummaryFilterRequest request);
    }
}
